"""Prepare and promote the approved DateTime pilot, with exact-version recovery."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from tempfile import TemporaryDirectory
import time
from urllib.error import HTTPError
from urllib.parse import urlsplit
from urllib.request import urlopen
import xml.etree.ElementTree as ET
from zipfile import BadZipFile, ZipFile

ID = "Broiler.DateTime"
PRODUCER = "Broiler-Platform/Broiler.DateTime"
AGGREGATE = "Broiler-Platform/Broiler"
WORKFLOW = ".github/workflows/nuget-pilot-matrix.yml"
FEED = "https://api.nuget.org/v3/index.json"
PREVIEW = re.compile(r"\d+\.\d+\.\d+-preview\.[1-9]\d*")
SHA = re.compile(r"[0-9a-f]{40}")
REQUIRED_CONTROLS = {"wrong packed dependency range", "missing staged DOM",
                     "wrong source for identical DOM archive", "failing producer suite"}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    draft = path.with_suffix(".tmp")
    draft.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    draft.replace(path)


def process(args, cwd, env=None, secret=""):
    try:
        result = subprocess.run(args, cwd=cwd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                text=True, encoding="utf-8", errors="replace", timeout=160)
    except subprocess.TimeoutExpired as error:
        raise ValueError(f"{' '.join(args[:2])} timed out") from error
    if result.returncode:
        details = result.stdout.replace(secret, "***") if secret else result.stdout
        raise ValueError(f"{' '.join(args[:2])} failed\n{details[-2400:]}")
    return result.stdout.strip()


def require_review(review, commit):
    if not SHA.fullmatch(commit):
        raise ValueError("Invalid reviewed source commit")
    text = review.read_text(encoding="utf-8")
    approved = re.search(r"^> \*\*Status: APPROVED FOR (?:FIRST )?PREVIEW\.\*\*\s*$", text, re.M)
    recorded = re.findall(r"^- \*\*Commit:\*\* \x60([0-9a-f]{40})\x60\s*$", text, re.M)
    reviewer = re.search(r"^- \*\*Reviewer:\*\* +(\S.*)$", text, re.M)
    if not approved or recorded != [commit] or not reviewer:
        detail = recorded[0] if len(recorded) == 1 else "unavailable"
        raise ValueError(f"DateTime human approval must name the exact source commit {commit}. Recorded commit: {detail}.")


def validate_run(run):
    if (run.get("path") != WORKFLOW or run.get("event") != "workflow_dispatch" or
            run.get("head_branch") != "main" or run.get("conclusion") != "success" or
            run.get("head_repository", {}).get("full_name") != AGGREGATE or
            not SHA.fullmatch(run.get("head_sha", "")) or
            not re.fullmatch(r"[1-9]\d*", str(run.get("id", "")))):
        raise ValueError("Expected a successful pilot matrix run from the aggregate default branch")


def pick_candidate(reports):
    if len(reports) != 2 or {row["profile"]["os"] for row in reports} != {"Windows", "Linux"}:
        raise ValueError("Promotion requires the Windows and Linux profiles")
    candidates = []
    for row in reports:
        if (row["status"] != "passed" or row.get("dateTimeReleaseIntent") is not True or
                "Broiler.DateTime" in row["edits"] or
                {item["check"] for item in row["negativeControls"] if item["rejected"]} != REQUIRED_CONTROLS or
                len(row["packageConsumers"]) != 5 or
                any(item["status"] != "passed" for item in row["packageConsumers"])):
            raise ValueError("Matrix did not validate an explicitly staged release from committed DateTime sources")
        producer = row["producers"][ID]
        probe = next((item for item in row["packageConsumers"] if item["consumer"] == ID), None)
        if (producer["tests"] <= 0 or len(producer["packages"]) != 1 or not probe or
                len(probe["resolved"]) != 1 or not SHA.fullmatch(producer["commit"])):
            raise ValueError("Missing DateTime producer or consumer evidence")
        package = producer["packages"][0]
        if (package["id"] != ID or not PREVIEW.fullmatch(package["version"]) or
                package["version"] != producer["version"] or package["dependencyGroups"] != {"net10.0": {}} or
                package["file"] != f"{ID}.{package['version']}.nupkg" or
                package["symbolsFile"] != f"{ID}.{package['version']}.snupkg" or
                probe["resolved"][0]["sha512"] != package["sha512"] or
                probe["resolved"][0]["source"] != "candidate-feed" or
                probe["resolved"][0]["version"] != package["version"] or probe["resolved"][0]["package"] != ID):
            raise ValueError("DateTime package identity or probe evidence does not match the pilot contract")
        candidates.append((row["profile"]["os"], producer["commit"], package))
    if candidates[0][1] != candidates[1][1] or candidates[0][2]["version"] != candidates[1][2]["version"]:
        raise ValueError("Validated profiles disagree on the release commit or version")
    _, commit, package = next(row for row in candidates if row[0] == "Linux")
    return commit, package


def metadata(path):
    with ZipFile(path) as archive:
        name, = [entry for entry in archive.namelist() if entry.lower().endswith(".nuspec")]
        data = ET.fromstring(archive.read(name)).find("{*}metadata")
        if data is None:
            raise ValueError("Package lacks nuspec metadata")
        return data


def match_archive(folder, package, commit):
    path = folder / package["file"]
    symbols = folder / package["symbolsFile"]
    for file, expected in ((path, package["sha256"]), (symbols, package["symbolsSha256"])):
        if digest(file.read_bytes()) != expected:
            raise ValueError(f"Artifact digest mismatch for {file.name}")
    data = metadata(path)
    repo = data.find("{*}repository")
    if (data.findtext("{*}id") != ID or data.findtext("{*}version") != package["version"] or
            repo is None or repo.get("type") != "git" or repo.get("commit") != commit or
            repo.get("url", "").removesuffix(".git") != f"https://github.com/{PRODUCER}" or
            any(item.tag.split("}")[-1] == "dependency" for item in data.iter())):
        raise ValueError("Candidate nuspec differs from the reviewed identity, source commit or contract")
    return path, symbols


def prepare(args):
    run = json.loads(args.run.read_text(encoding="utf-8"))
    validate_run(run)
    commit, package = pick_candidate(json.loads(args.matrix.read_text(encoding="utf-8")))
    require_review(args.repository / "HUMAN_REVIEW.md", commit)
    process(["git", "merge-base", "--is-ancestor", commit, "HEAD"], args.repository)
    files = match_archive(args.packages, package, commit)
    args.output.mkdir(parents=True)
    for file in files:
        shutil.copyfile(file, args.output / file.name)
    plan = {"sourceRun": run["id"], "aggregateCommit": run["head_sha"], "producerCommit": commit,
            "reviewSha256": digest((args.repository / "HUMAN_REVIEW.md").read_bytes()),
            "packages": [{**package, "prerequisites": []}]}
    path = args.output / "promotion.json"
    write_json(path, plan)
    sha = digest(path.read_bytes())
    print(f"Prepared {ID} {package['version']} from {commit}\nPromotion manifest SHA-256 {sha}")
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
            output.write(f"manifest_sha256={sha}\nversion={package['version']}\nproducer_commit={commit}\n")


def payload(path):
    result = {}
    with ZipFile(path) as archive:
        names = set()
        for entry in archive.infolist():
            name = entry.filename
            if name in names:
                raise ValueError(f"Duplicate archive member {name}")
            names.add(name)
            if entry.is_dir() or name == ".signature.p7s":
                continue
            content = archive.read(entry)
            if name == "[Content_Types].xml":
                root = ET.fromstring(content)
                for item in list(root):
                    if (item.tag.split("}")[-1] == "Default" and item.get("Extension") == "p7s" and
                            item.get("ContentType") == "application/octet-stream"):
                        root.remove(item)
                # OPC signing may rewrite or reorder the content type declarations.
                content = repr(sorted((item.tag, sorted(item.attrib.items())) for item in root)).encode()
            result[name] = digest(content)
    return result


def nuget_bytes(url, missing=False):
    host = urlsplit(url)
    if host.scheme != "https" or host.netloc != "api.nuget.org":
        raise ValueError("Unexpected NuGet.org service URL")
    try:
        with urlopen(url, timeout=20) as response:
            return response.read()
    except HTTPError as error:
        if missing and error.code == 404:
            return None
        raise


def config(work, fingerprints):
    root = ET.Element("configuration")
    sources = ET.SubElement(root, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="nuget.org", value=FEED)
    ET.SubElement(ET.SubElement(root, "fallbackPackageFolders"), "clear")
    ET.SubElement(ET.SubElement(root, "config"), "add", key="signatureValidationMode", value="require")
    signers = ET.SubElement(root, "trustedSigners")
    ET.SubElement(signers, "clear")
    trusted = ET.SubElement(signers, "repository", name="nuget.org", serviceIndex=FEED)
    for fingerprint in fingerprints:
        ET.SubElement(trusted, "certificate", fingerprint=fingerprint, hashAlgorithm="SHA256",
                      allowUntrustedRoot="false")
    path = work / "NuGet.config"
    ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)
    return path


class PublicFeed:
    def __init__(self, repo, artifact):
        self.repo, self.artifact = repo, artifact
        resources = {row["@type"]: row["@id"] for row in json.loads(nuget_bytes(FEED))["resources"]}
        self.flat = resources["PackageBaseAddress/3.0.0"].rstrip("/")
        signed = json.loads(nuget_bytes(resources["RepositorySignatures/5.0.0"]))
        self.fingerprints = sorted({row["fingerprints"]["2.16.840.1.101.3.4.2.1"]
                                    for row in signed["signingCertificates"]})
        if (not signed.get("allRepositorySigned") or not self.fingerprints or
                not all(re.fullmatch(r"[0-9a-fA-F]{64}", value) for value in self.fingerprints)):
            raise ValueError("NuGet.org repository signing information was incomplete")

    def find(self, package):
        pid, version = package["id"].lower(), package["version"].lower()
        return nuget_bytes(f"{self.flat}/{pid}/{version}/{pid}.{version}.nupkg", missing=True)

    def push(self, package):
        key = os.environ.get("NUGET_API_KEY")
        if not key:
            raise ValueError("NuGet Trusted Publishing did not provide a short-lived key")
        suffix = package["version"].split("-", 1)[1]
        with TemporaryDirectory() as temp:
            version_output = Path(temp) / "version.txt"
            env = {**os.environ, "VERSION_SUFFIX": suffix, "GITHUB_EVENT_NAME": "workflow_dispatch",
                   "GITHUB_OUTPUT": str(version_output)}
            env.pop("NUGET_API_KEY", None)
            process(["node", "eng/resolve-preview-version.mjs"], self.repo, env, secret=key)
            if version_output.read_text(encoding="utf-8").strip() != f"version={package['version']}":
                raise ValueError("The producer version policy rejected the staged version")
        process(["dotnet", "nuget", "push", str(self.artifact / package["file"]), "--source", FEED,
                 "--api-key", key, "--symbol-api-key", key, "--timeout", "120"], self.repo, secret=key)

    def signature(self, path, work, conf):
        fingerprints = [arg for fingerprint in self.fingerprints
                        for arg in ("--certificate-fingerprint", fingerprint.upper())]
        process(["dotnet", "nuget", "verify", str(path), "--all", "--configfile", str(conf),
                 *fingerprints], work)

    def verify(self, package, served):
        with TemporaryDirectory(prefix="nuget-public-") as temp:
            work = Path(temp)
            signed = work / "public.nupkg"
            signed.write_bytes(served)
            conf = config(work, self.fingerprints)
            self.signature(signed, work, conf)
            data = metadata(signed)
            if data.findtext("{*}id") != package["id"] or data.findtext("{*}version") != package["version"]:
                raise ValueError("Public feed returned a different package identity")
            if payload(signed) != payload(self.artifact / package["file"]):
                raise ValueError("Public package payload differs from the approved candidate")
            return self.smoke(package, served, work, conf)

    def smoke(self, package, served, work, conf):
        cache = work / "packages"
        project = work / "PublicConsumer.csproj"
        project.write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="{package['id']}" Version="[{package['version']}]" /></ItemGroup>
</Project>
""", encoding="utf-8")
        (work / "Program.cs").write_text(
            'return Broiler.DateTime.ExtendedIsoDateTime.Parse("2024-02-29T00:00:00Z").Year == 2024 ? 0 : 1;\n',
            encoding="utf-8")
        env = {**os.environ, "NUGET_PACKAGES": str(cache), "DisableImplicitLibraryPacksFolder": "true",
               "NUGET_HTTP_CACHE_PATH": str(work / "http"), "MSBUILDDISABLENODEREUSE": "1",
               "DOTNET_CLI_USE_MSBUILD_SERVER": "0", "UseSharedCompilation": "false"}
        env.pop("NUGET_API_KEY", None)
        process(["dotnet", "restore", str(project), "--configfile", str(conf), "--packages", str(cache),
                 "--no-http-cache", "--nologo"], work, env)
        assets = json.loads((work / "obj/project.assets.json").read_text(encoding="utf-8-sig"))
        identity, version = package["id"], package["version"]
        resolved = assets["targets"]["net10.0"][f"{identity}/{version}"]
        folder = cache / identity.lower() / version
        restored = json.loads((folder / ".nupkg.metadata").read_text(encoding="utf-8"))
        restored_archive = folder / f"{identity.lower()}.{version}.nupkg"
        content_hash = assets["libraries"][f"{identity}/{version}"]["sha512"]
        if (set(assets["project"]["restore"]["sources"]) != {FEED} or
                resolved["type"] != "package" or not all(resolved.get(kind) for kind in ("compile", "runtime")) or
                restored["source"] != FEED or not content_hash or restored["contentHash"] != content_hash or
                restored_archive.read_bytes() != served):
            raise ValueError("Public restore did not resolve the exact signed package from NuGet.org")
        process(["dotnet", "run", "--project", str(project), "--no-restore", "-c", "Release"], work, env)
        return {"framework": "net10.0", "publicArchiveSha256": digest(served),
                "nugetContentHash": content_hash, "source": FEED}


def promote(plan, backend, state_path, plan_hash, checks=18, pause=7):
    packages = plan["packages"]
    if not packages:
        raise ValueError("Release manifest contains no packages")
    status = {"manifestSha256": plan_hash, "sourceRun": plan["sourceRun"], "packages": {}}
    states = status["packages"]
    seen = set()
    for package in packages:
        identity = package["id"].casefold()
        if identity in seen or any(key.casefold() not in seen for key in package["prerequisites"]):
            raise ValueError("Release manifest is not in unique prerequisite-first order")
        seen.add(identity)
        states[identity] = {"version": package["version"], "state": "pending", "uploadAttempted": False}
    write_json(state_path, status)

    for package in packages:
        key = package["id"].casefold()
        current = states[key]

        def mark(state, **details):
            current.update(state=state, **details)
            write_json(state_path, status)
            print(f"{package['id']} {package['version']}: {state}", flush=True)

        needed = [name for name in package["prerequisites"] if states[name.casefold()]["state"] != "verified"]
        if needed:
            mark("blocked", prerequisites=needed)
            continue
        try:
            served = backend.find(package)
            if served is None:
                mark("uploading", uploadAttempted=True)
                try:
                    backend.push(package)
                except (ValueError, OSError) as error:
                    # An interrupted or duplicate push may already be indexing.
                    mark("awaiting-index", pushError=str(error))
                else:
                    mark("awaiting-index")
                for attempt in range(checks):
                    if attempt:
                        time.sleep(pause)
                    try:
                        served = backend.find(package)
                    except OSError as error:
                        mark("awaiting-index", lastLookupError=str(error))
                    if served is not None:
                        break
                if served is None:
                    mark("awaiting-index", reason="Exact version did not become visible before the timeout")
                    continue
            else:
                mark("reconciling-existing", alreadyPresent=True)
            evidence = backend.verify(package, served)
            mark("verified", evidence=evidence)
        except (OSError, ValueError, KeyError, BadZipFile, ET.ParseError) as error:
            mark("failed", reason=str(error))
    return all(row["state"] == "verified" for row in states.values())


def publish(args):
    path = args.artifact / "promotion.json"
    plan_hash = digest(path.read_bytes())
    if plan_hash != args.expected_sha:
        raise ValueError("Prepared plan differs from the digest sent for approval")
    plan = json.loads(path.read_text(encoding="utf-8"))
    commit = plan["producerCommit"]
    review = args.repository / "HUMAN_REVIEW.md"
    require_review(review, commit)
    if digest(review.read_bytes()) != plan["reviewSha256"]:
        raise ValueError("Human review record changed after preparation")
    package, = plan["packages"]
    if (package["id"] != ID or not PREVIEW.fullmatch(package["version"]) or package["prerequisites"] or
            package["file"] != f"{ID}.{package['version']}.nupkg" or
            package["symbolsFile"] != f"{ID}.{package['version']}.snupkg"):
        raise ValueError("Only the independent DateTime pilot can be promoted")
    match_archive(args.artifact, package, commit)
    return int(not promote(plan, PublicFeed(args.repository, args.artifact), args.state, plan_hash))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="mode", required=True)
    stage = sub.add_parser("prepare")
    for name in ("run", "matrix", "packages", "output", "repository"):
        stage.add_argument(f"--{name}", type=lambda path: Path(path).resolve(), required=True)
    release = sub.add_parser("publish")
    for name in ("artifact", "repository", "state"):
        release.add_argument(f"--{name}", type=lambda path: Path(path).resolve(), required=True)
    release.add_argument("--expected-sha", required=True)
    args = parser.parse_args()
    try:
        if args.mode == "prepare":
            prepare(args)
            return 0
        return publish(args)
    except (OSError, ValueError, KeyError, BadZipFile, ET.ParseError, subprocess.SubprocessError) as error:
        print(str(error), flush=True)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

"""Publishes LocalExile release zips to GitHub (idempotent): one release per version, its zip + .sha256 as assets.

Reads the token from GH_TOKEN, the notes from 更新说明.md, the zips from artifacts/release.
Usage: python publish-release.py 0.8.92-merge [more …]
"""
import json
import os
import re
import sys
import urllib.parse
import urllib.request

TOKEN = os.environ["GH_TOKEN"]
REPO = "mingyue8621/LocalExile"
API = "https://api.github.com"
ROOT = r"G:\LocalExile"
RELEASE_DIR = os.path.join(ROOT, "artifacts", "release")
NOTES = os.path.join(ROOT, "更新说明.md")


def call(method, url, payload=None):
    data = None if payload is None else json.dumps(payload).encode()
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", "Bearer " + TOKEN)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("User-Agent", "localexile-release")
    if data is not None:
        req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=300) as r:
        body = r.read()
    return json.loads(body.decode()) if body else {}


def section(version):
    text = open(NOTES, encoding="utf-8").read()
    start = text.index("## " + version)
    rest = text[start + 3:]
    end = rest.find("\n## ")
    return rest if end < 0 else rest[:end]


def upload(upload_url, path, name):
    url = upload_url.split("{")[0] + "?name=" + urllib.parse.quote(name)
    size = os.path.getsize(path)
    with open(path, "rb") as handle:
        req = urllib.request.Request(url, data=handle, method="POST")
        req.add_header("Authorization", "Bearer " + TOKEN)
        req.add_header("Accept", "application/vnd.github+json")
        req.add_header("User-Agent", "localexile-release")
        req.add_header("Content-Type", "application/octet-stream")
        req.add_header("Content-Length", str(size))
        with urllib.request.urlopen(req, timeout=3600) as r:
            return json.loads(r.read().decode())


def main():
    for version in sys.argv[1:]:
        tag = "v" + version
        zip_name = "LocalExile-v{}-win-x64.zip".format(version)
        zip_path = os.path.join(RELEASE_DIR, zip_name)
        sha_path = zip_path + ".sha256"
        if not os.path.exists(zip_path):
            print(version, "没有 zip", flush=True)
            continue
        try:
            release = call("GET", "{}/repos/{}/releases/tags/{}".format(API, REPO, tag))
            print(version, "已存在", flush=True)
        except urllib.error.HTTPError as e:
            if e.code != 404:
                raise
            release = call("POST", "{}/repos/{}/releases".format(API, REPO),
                           {"tag_name": tag, "name": version, "body": section(version), "draft": False, "prerelease": False})
            print(version, "已创建", flush=True)
        have = {a["name"] for a in release.get("assets", [])}
        for path, name in ((zip_path, zip_name), (sha_path, zip_name + ".sha256")):
            if name in have:
                print(version, name, "已在", flush=True)
                continue
            upload(release["upload_url"], path, name)
            print(version, name, "已上传", flush=True)


if __name__ == "__main__":
    main()

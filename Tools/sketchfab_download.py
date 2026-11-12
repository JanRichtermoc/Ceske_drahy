#!/usr/bin/env python3
"""Download an authorized Sketchfab model; credentials never enter the project."""
import argparse
import getpass
import io
import json
import os
from pathlib import Path
import ssl
import urllib.error
import urllib.request
import zipfile

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("uid", help="Sketchfab model UID")
    parser.add_argument("--output", default=".local/sketchfab-download")
    args = parser.parse_args()
    if len(args.uid) != 32 or any(c not in "0123456789abcdef" for c in args.uid.lower()):
        parser.error("UID must contain 32 hexadecimal characters")
    token = os.environ.get("SKETCHFAB_API_TOKEN") or getpass.getpass("Sketchfab API token: ")
    context = ssl.create_default_context(cafile="/etc/ssl/cert.pem" if Path("/etc/ssl/cert.pem").exists() else None)
    def get_json(path, authenticated=False):
        headers = {"Authorization": "Token " + token} if authenticated else {}
        req = urllib.request.Request("https://api.sketchfab.com/v3/" + path, headers=headers)
        with urllib.request.urlopen(req, context=context, timeout=45) as response:
            return json.load(response)
    model = get_json("models/" + args.uid)
    if not model.get("isDownloadable"):
        raise SystemExit("The author has not enabled downloads for this model.")
    info = get_json("models/" + args.uid + "/download", True)
    target = Path(args.output).resolve()
    target.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(info["gltf"]["url"], context=context, timeout=90) as response:
        archive = zipfile.ZipFile(io.BytesIO(response.read()))
    for entry in archive.infolist():
        destination = (target / entry.filename).resolve()
        if not destination.is_relative_to(target):
            raise SystemExit("Archive contains an unsafe path")
    archive.extractall(target)
    attribution = {"name": model["name"], "author": model["user"]["displayName"],
                   "url": model["viewerUrl"], "license": model["license"]["url"], "uid": args.uid}
    (target / "attribution.json").write_text(json.dumps(attribution, indent=2), encoding="utf-8")
    print(f"Downloaded {model['name']} to {target}. License: {model['license']['label']}")

if __name__ == "__main__":
    try:
        main()
    except urllib.error.HTTPError as error:
        raise SystemExit(f"Sketchfab returned HTTP {error.code}; check the token and download permission.")

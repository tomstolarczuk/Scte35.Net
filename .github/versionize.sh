#!/usr/bin/env bash

DID_RELEASE="false"
RELEASE_TAG=""
VERSION=""

git fetch --tags --prune || true

set +e
versionize --ignore-insignificant-commits
EC=$?
set -e

if [[ $EC -eq 0 ]]; then
  git push origin HEAD:master --follow-tags
  RELEASE_TAG="$(git tag --points-at HEAD | head -n1 || true)"
  DID_RELEASE="true"
else
  echo "No version bump (no conventional commits since last tag)."
fi

if [[ -n "$RELEASE_TAG" ]]; then
  VERSION="${RELEASE_TAG#v}"
elif git describe --tags --abbrev=0 >/dev/null 2>&1; then
  TAG="$(git describe --tags --abbrev=0)"
  VERSION="${TAG#v}"
else
  VERSION="0.0.0-ci.${GITHUB_RUN_NUMBER:-0}+sha.${GITHUB_SHA:-0000000:0:7}"
fi

{
  echo "VERSION=$VERSION"
  echo "DID_RELEASE=$DID_RELEASE"
  echo "RELEASE_TAG=$RELEASE_TAG"
} > version.env

echo "Resolved VERSION=$VERSION (DID_RELEASE=$DID_RELEASE, TAG=$RELEASE_TAG)"
#!/usr/bin/env bash
set -euo pipefail

DID_RELEASE="false"
RELEASE_TAG=""
VERSION=""

export PATH="$PATH:/root/.dotnet/tools"
git fetch --tags --prune

git config user.name  "ci-bot"
git config user.email "ci-bot@gitlab"
git remote set-url origin "https://gitlab-ci-token:${CI_JOB_TOKEN}@${CI_SERVER_HOST}/${CI_PROJECT_PATH}.git"

if [[ "${CI_COMMIT_BRANCH:-}" == "${CI_DEFAULT_BRANCH:-}" ]]; then
  echo "Running versionize on default branch..."
  set +e
  versionize
  EC=$?
  set -e
  if [[ $EC -eq 0 ]]; then
    git push origin HEAD:"$CI_COMMIT_REF_NAME" --follow-tags
    RELEASE_TAG="$(git tag --points-at HEAD | head -n1)"
    DID_RELEASE="true"
  else
    echo "No version bump (no conventional commits since last tag)."
  fi
else
  echo "Skipping versionize (not default branch)."
fi

if [[ -n "$RELEASE_TAG" ]]; then
  VERSION="${RELEASE_TAG#v}"
elif git describe --tags --abbrev=0 >/dev/null 2>&1; then
  TAG="$(git describe --tags --abbrev=0)"
  VERSION="${TAG#v}"
else
  VERSION="0.0.0-ci.${CI_PIPELINE_IID:-0}+sha.${CI_COMMIT_SHORT_SHA:-0000000}"
fi

echo "Resolved VERSION=$VERSION (DID_RELEASE=$DID_RELEASE, TAG=$RELEASE_TAG)"
{
  echo "VERSION=$VERSION"
  echo "PACKAGE_VERSION=$VERSION"
  echo "DID_RELEASE=$DID_RELEASE"
  echo "RELEASE_TAG=$RELEASE_TAG"
} > version.env
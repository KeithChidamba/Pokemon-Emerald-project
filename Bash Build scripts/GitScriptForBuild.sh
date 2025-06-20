#!/bin/bash

# Configuration
GIT_USERNAME="KeithChidamba"
GIT_PASSWORD="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"
REPO_URL="https://github.com/KeithChidamba/Pokemon-Emerald-Build.git"
TARGET_DIR="/home/keith/Pokemon Luminary/Build"
COMMIT_MSG="${1:-Auto-commit}"

# Go to project directory
cd "$TARGET_DIR" || { echo "Directory not found"; exit 1; }

# Configure Git URL with credentials
CRED_URL="https://${GIT_USERNAME}:${GIT_PASSWORD}@${REPO_URL#https://}"

# Git operations
git add .
git commit -m "$COMMIT_MSG"
git push "$CRED_URL" main -f

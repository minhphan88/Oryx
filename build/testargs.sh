#!/bin/bash
# --------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.
# --------------------------------------------------------------------------------------------

set -ex

declare -r REPO_DIR=$( cd $( dirname "$0" ) && cd .. && pwd )

# Load all variables
source $REPO_DIR/build/__variables.sh
source $REPO_DIR/build/__functions.sh
source $REPO_DIR/build/__sdkStorageConstants.sh

echo $1
echo $2
echo $3

firstArg="$1"
secondArg="$2"
thirdArg='$3'

if [[ -n "$firstArg" ]]; then
    echo "First Arg is: $firstArg"
fi

if [[ -n "$secondArg" ]]; then
    echo "Second Arg is: $secondArg"
fi

if [[ -n "$thirdArg" ]]; then
    echo "Third Arg is: $thirdArg"
fi

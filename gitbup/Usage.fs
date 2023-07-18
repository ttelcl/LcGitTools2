﻿// (c) 2023  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detail =
  cp "\fogitbup\f0 - git backup utility\f0"
  cp ""
  cp "Common options:"
  cp "\fg-v\f0\fx\fx               Verbose mode"
  cp "\fg-C\f0 \fc<repopath>\f0    Run gitbup in the specified folder"
  cp ""
  cp "\fogitbup run\f0 [\fg-tier \fc<n>\f0]"
  cp "   Create a new backup bundle using the configured settings."
  cp "   \fg-tier \fcauto\f0  (default) Automatically select the backup tier"
  cp "   \fg-tier \fc<n>\f0   Override the backup tier (\fg-tier \fc0\f0 for full backup)"
  cp ""
  cp "\fogitbup anchor\f0 [\fg-list\f0|\fg-set \fc<tag>\f0 \fc<folder>\f0]"
  cp "   Manage anchor folders (for use by any repository)"
  cp "   \fg-list\f0       List the defined anchor folders"
  cp "   \fg-set\f0        Add / change / delete an anchor folder."
  cp ""
  cp "\fogitbup name\f0 [\fg-show\f0|\fg-set \fc<name>\f0|\fg-default\f0]"
  cp "   Manage the name for this repository (determining folder and file names)"
  cp "   \fg-default\f0    Reset the name to the default, based on the repo folder name."
  cp "   \fg-set\f0        Change the name"
  cp ""
  cp "\fogitbup folder\f0 [\fg-show\f0|\fg-anchor \fc<tag>\f0|\fg-local\f0]"
  cp "   Manage the backup folder for the current repository. Aliases: \fodirectory\f0, \fotarget\f0"
  cp "   \fg-show\f0       Show the currently configured destination folder."
  cp "   \fg-anchor\f0     Change the destination to a child of one of the anchor folders"
  cp "   \fg-local\f0      Change the destination to a folder inside the git folder itself"
  



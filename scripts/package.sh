#!/bin/bash

set -eE -o functrace

plugin_name="VarLicenseFilter"
work_dir="publish"

failure() {
  rm -rf "$work_dir"
  local lineno="$1"
  local msg="$2"
  echo "Failed at $lineno: $msg"
}
trap 'failure ${LINENO} "$BASH_COMMAND"' ERR

prepare_directories() {
  mkdir -p "$work_dir"
  resource_dir="$work_dir/Custom/Scripts/everlaster/$plugin_name"
  mkdir -p "$resource_dir"
  cp meta.json "$work_dir/"
}

package_files() {
  file="$plugin_name.cslist"
  cp "$file" "$resource_dir/"
  while IFS= read -r line; do
    line=$(echo "$line" | sed 's/\r$//')
    line="${line//\\//}"
    filename=$(basename "$line")
    from_dir=$(dirname "$line")
    to_dir=$(dirname "$line" | sed 's#Common/src#src/Common#')
    mkdir -p "$resource_dir/$to_dir"
    cp "$from_dir/$filename" "$resource_dir/$to_dir/"
  done < "$file"

  # Update the .cslist file to reference the updated Common file paths
  sed -i 's#\Common\\src#src\\Common#g' "$resource_dir/$(basename "$file")"
}

update_version_info() {
  sed -i "s/0\.0\.0/$plugin_version/g" "$work_dir/meta.json"
  sed -i "s/0\.0\.0/$plugin_version/g" "$resource_dir/src/Common/Script.cs"
}

hide_files() {
  for file in $(find "$resource_dir" -type f -name "*.cs"); do
    sed -i "s/^#define .*/\/\//" "$file"
    touch "$file.hide"
  done
}

finalize_and_move_package() {
  printf "Creating package...\n"
  package_file="everlaster.$plugin_name.$package_version.var"
  cd "$work_dir"
  zip -rq "$package_file" ./*
  printf "Package %s created for plugin version v%s.\n" "$package_file" "$plugin_version"
  addon_packages_dir="../../../../../AddonPackages/Self"
  mkdir -p "$addon_packages_dir"
  mv "$package_file" "$addon_packages_dir"
  printf "Package %s moved to AddonPackages/Self.\n" "$package_file"
  cd ..
  rm -rf "$work_dir"
}

main() {
  package_version="$1"
  if [ -z "package_version" ]; then
    printf "Usage: ./package.sh [package version]\n" && exit 1
  fi

  plugin_version=$(git describe --tags --match "v*" --abbrev=0 HEAD 2>/dev/null | sed s/v//)
  if [ -z "$plugin_version" ]; then
    printf "Git tag not set on current commit." && exit 1
  fi

  prepare_directories
  package_files
  update_version_info
  hide_files
  finalize_and_move_package
}

main "$@"

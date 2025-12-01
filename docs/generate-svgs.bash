#!/bin/bash
set -e
while read line; do
    file=$(sed -r 's:\t+:\t:g' <<<"$line" | cut -f1)
    value=$(sed -r 's:\t+:\t:g' <<<"$line" | cut -f2)
    prefix=rgb
    if [ $(awk -F, 'END {print NF}' <<<"$value") -eq 4 ]; then
        prefix=rgba;
    fi
    cat >"$1/$file.svg" << EOF
<svg xmlns="http://www.w3.org/2000/svg" width="64" height="32" >
  <rect width="64" height="32" fill="$prefix$value" ></rect>
</svg>
EOF
    echo "$1/$file.svg"
done

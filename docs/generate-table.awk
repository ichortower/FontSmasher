BEGIN {
    print "<table>";
}

{
    i = NR-1;
    names[i] = $1;
    values[i] = $2;
}

function ceil(x) {
    return int(x) + (x > int(x));
}

function color(name, value) {
    if (name == "") {
        return;
    }
    printf "<td align=\"right\"><code>%s</code><br><code>%s</code></td>\n", name, value;
    printf "<td><img src=\"svg/%s.svg\"></td>\n", name;
}

END {
    step = ceil(NR/3);
    for (i = 0; i < step; ++i) {
        print "<tr>";
        color(names[i], values[i]);
        color(names[i+step], values[i+step]);
        color(names[i+2*step], values[i+2*step]);
        print "</tr>";
    }
    print "</table>";
}

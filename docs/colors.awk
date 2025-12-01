BEGIN {
    print "<table>";
}

{
    i = NR-1;
    names[i] = $1;
    values[i] = $2;
}

function cell(name, value) {
    printf "<td><code>%s</code></td><td>rgb%s</td><td style=\"background-color:rgb%s;\" ></td>",
            name, value, value;
}

END {
    step = NR/3;
    for (i = 0; i < step; ++i) {
        printf "<tr>";
        cell(names[i], values[i]);
        cell(names[i+step], values[i+step]);
        cell(names[i+2*step], values[i+2*step]);
        print "</tr>";
    }
    print "</table>";
}

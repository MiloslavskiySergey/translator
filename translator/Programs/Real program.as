dim continue $;
continue as true;
while continue do
    dim action %;
    write("Select action\n");
    write("1 - calculate factorial\n");
    write("2 - calculate quadratic equation\n");
    write("3 - exit\n");
    read(action);
    if action = 1 then
        dim n, i, f %;
        write("Enter n: ");
        read(n);
        f as 1;
        for i as 2 to n do
            f as f * i;
        endfor
        write("Factorial = ", f, "\n");
    else if action = 2 then
        dim a, b, c, d, od, l, r !;
        write("Enter a: ");
        read(a);
        write("Enter b: ");
        read(b);
        write("Enter c: ");
        read(c);
        d as b * b - 4 * a * c;
        l as (0 - b) / 2 * a;
        if d > 0 then
            r as d ^ 0.5 / (2 * a);
            write("x1 = ", l + r, "; ", "x2 = ", l - r, "\n");
        else if d = 0 then
            write("x = ", l, "\n");
        else
            od as 0 - d;
            r as od ^ 0.5 / (2 * a);
            write("x1 = ", l, " + ", r, " * i", "; ", "x2 = ", l, " - ", r, " * i", "\n");
        endif
    else if action = 3 then
        continue as false;
    else
        write("Wrong action\n");
    endif
endwhile
end
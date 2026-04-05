// HC-SR04 mounting plate

base_w = 45;
base_d = 14;
base_h = 2;

side_w = 15.5;
side_d = 12;
side_h = 1.95;

union() {
    // Base plate
    cube([base_w, base_d, base_h], center = false);

    // Left raised section
    translate([0, 0, base_h])
        cube([side_w, side_d, side_h], center = false);

    // Right raised section
    translate([base_w - side_w, 0, base_h])
        cube([side_w, side_d, side_h], center = false);
}
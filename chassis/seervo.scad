// Outer box dimensions
outer_w = 70;
outer_l = 120;
outer_h = 35;

// Inner battery cavity
inner_w = 53;
inner_h = 32;
front_wall = 3;      // wall thickness at the front
back_overlap = 0.2;  // slight protrusion past the back for a clean cut

// Motor cutouts
motor_cutout_l = 23;     // along front-to-back dimension
motor_cutout_depth = 5;  // into the side walls
corner_offset = 3;       // distance from each end along length
side_overlap = 0.2;      // protrusion beyond outer face for clean cut

// Motor mounting holes
motor_hole_d = 2.5;
motor_hole_r = motor_hole_d / 2;
motor_hole_z = 4;        // 4mm up from bottom
hole_offset_in_cutout = 2.75;
hole_overlap = 0.2;      // small stub for clean through cut

// Top circuit board mount
// 58 x 82 is the OUTER footprint of the mount.
// The 5mm walls extend inward from that footprint.
board_w = 58;
board_l = 82;
board_wall = 5;
board_wall_h = 5;
side_wire_gap = 10;      // gap at front and back of each long side

// Circuit board standoffs
standoff_inset = 4.5;    // from each corner of the 58 x 82 board footprint
standoff_d = 4;
standoff_r = standoff_d / 2;
standoff_h = 10;         // height from top of battery box

// Camera mast
mast_w = 24.5;           // width dimension
mast_l = 5;              // front-to-back dimension
mast_h = 65;             // from top of battery box

// Camera mast holes
mast_hole_d = 3;
mast_hole_r = mast_hole_d / 2;
mast_hole_side_inset = 2; // center is 2mm from each side
mast_hole_top_inset = 2;  // center is 2mm down from top
mast_hole_overlap = 0.2;  // clean through cut

front_cutout_y = corner_offset;
rear_cutout_y  = outer_l - corner_offset - motor_cutout_l;

// Centered position of the board footprint on top of the chassis
board_x0 = (outer_w - board_w) / 2;
board_y0 = (outer_l - board_l) / 2;

// Camera mast position:
// centered in the gap between the back of the board mount footprint
// and the back of the battery box
mast_x0 = (outer_w - mast_w) / 2;
mast_y0 = board_y0 + board_l + ((outer_l - (board_y0 + board_l)) - mast_l) / 2;

// Hole centers along the length dimension
motor_hole_y_positions = [
    front_cutout_y + hole_offset_in_cutout,
    front_cutout_y + motor_cutout_l - hole_offset_in_cutout,
    rear_cutout_y  + hole_offset_in_cutout,
    rear_cutout_y  + motor_cutout_l - hole_offset_in_cutout
];

// Hole centers for the mast, measured on the mast front/back face
mast_hole_x_positions = [
    mast_x0 + mast_hole_side_inset,
    mast_x0 + mast_w - mast_hole_side_inset
];
mast_hole_z = outer_h + mast_h - mast_hole_top_inset;

difference() {
    // Main body plus top board support walls, standoffs, and camera mast
    union() {
        // Outer box
        cube([outer_w, outer_l, outer_h], center = false);

        // Top walls for the circuit board mount
        // Overall footprint is 58 x 82 mm, centered on top.
        // Left and right walls have 10 mm gaps at front and back for wires.
        translate([board_x0, board_y0, outer_h]) {
            // Front wall
            cube([board_w, board_wall, board_wall_h], center = false);

            // Back wall
            translate([0, board_l - board_wall, 0])
                cube([board_w, board_wall, board_wall_h], center = false);

            // Left wall with front/back gaps
            translate([0, side_wire_gap, 0])
                cube([
                    board_wall,
                    board_l - 2 * side_wire_gap,
                    board_wall_h
                ], center = false);

            // Right wall with front/back gaps
            translate([board_w - board_wall, side_wire_gap, 0])
                cube([
                    board_wall,
                    board_l - 2 * side_wire_gap,
                    board_wall_h
                ], center = false);
        }

        // Circuit board standoffs
        for (x_pos = [board_x0 + standoff_inset, board_x0 + board_w - standoff_inset])
        for (y_pos = [board_y0 + standoff_inset, board_y0 + board_l - standoff_inset]) {
            translate([x_pos, y_pos, outer_h])
                cylinder(h = standoff_h, r = standoff_r, $fn = 48);
        }

        // Camera mounting mast
        translate([mast_x0, mast_y0, outer_h])
            cube([mast_w, mast_l, mast_h], center = false);
    }

    // Inner battery cavity
    translate([
        (outer_w - inner_w) / 2,
        front_wall,
        (outer_h - inner_h) / 2
    ])
    cube([
        inner_w,
        outer_l - front_wall + back_overlap,
        inner_h
    ], center = false);

    // Motor cutouts: left side
    translate([
        -side_overlap,
        front_cutout_y,
        -side_overlap
    ])
    cube([
        motor_cutout_depth + side_overlap,
        motor_cutout_l,
        outer_h + 2 * side_overlap
    ], center = false);

    translate([
        -side_overlap,
        rear_cutout_y,
        -side_overlap
    ])
    cube([
        motor_cutout_depth + side_overlap,
        motor_cutout_l,
        outer_h + 2 * side_overlap
    ], center = false);

    // Motor cutouts: right side
    translate([
        outer_w - motor_cutout_depth,
        front_cutout_y,
        -side_overlap
    ])
    cube([
        motor_cutout_depth + side_overlap,
        motor_cutout_l,
        outer_h + 2 * side_overlap
    ], center = false);

    translate([
        outer_w - motor_cutout_depth,
        rear_cutout_y,
        -side_overlap
    ])
    cube([
        motor_cutout_depth + side_overlap,
        motor_cutout_l,
        outer_h + 2 * side_overlap
    ], center = false);

    // Motor mounting holes: 4 cylinders through the full chassis width
    for (y_pos = motor_hole_y_positions) {
        translate([-hole_overlap, y_pos, motor_hole_z])
            rotate([0, 90, 0])
                cylinder(
                    h = outer_w + 2 * hole_overlap,
                    r = motor_hole_r,
                    $fn = 48
                );
    }

    // Camera mast holes: 2 holes through the mast front-to-back
    for (x_pos = mast_hole_x_positions) {
        translate([x_pos, mast_y0 - mast_hole_overlap, mast_hole_z])
            rotate([-90, 0, 0])
                cylinder(
                    h = mast_l + 2 * mast_hole_overlap,
                    r = mast_hole_r,
                    $fn = 48
                );
    }
}
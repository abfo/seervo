// Outer box dimensions
outer_w = 70;
outer_l = 120;
outer_h = 35;

// Inner battery cavity
inner_w = 53;
inner_h = 32;
front_wall = 3;
back_overlap = 0.2;

// Motor cutouts
motor_cutout_l = 23;
motor_cutout_depth = 5;
corner_offset = 3;
side_overlap = 0.2;

// Motor mounting pegs
motor_peg_d = 2.5;
motor_peg_r = motor_peg_d / 2;
motor_peg_z = 4;         // center 4mm up from bottom
motor_peg_len = 5;       // protrudes 5mm into motor cutout
peg_offset_in_cutout = 2.75;

// Top circuit board mount
board_w = 58;
board_l = 82;
board_wall = 5;
board_wall_h = 5;
side_wire_gap = 10;

// Circuit board standoffs
standoff_inset = 4.5;
standoff_d = 3;
standoff_r = standoff_d / 2;
standoff_h = 10;

// Camera mast
mast_w = 24.5;
mast_l = 5;
mast_h = 65;

// Camera mast holes
mast_hole_d = 3;
mast_hole_r = mast_hole_d / 2;
mast_hole_side_inset = 2;
mast_hole_top_inset = 2;
mast_hole_overlap = 0.2;

// Camera mast cutout
mast_cutout_w = mast_w / 2;
mast_cutout_h = mast_h / 2;
mast_cutout_overlap = 0.2;

front_cutout_y = corner_offset;
rear_cutout_y  = outer_l - corner_offset - motor_cutout_l;

// Board footprint position
board_x0 = (outer_w - board_w) / 2;
board_y0 = (outer_l - board_l) / 2;

// Camera mast position
mast_x0 = (outer_w - mast_w) / 2;
mast_y0 = board_y0 + board_l + ((outer_l - (board_y0 + board_l)) - mast_l) / 2;

// Motor peg centers along the length dimension
motor_peg_y_positions = [
    front_cutout_y + peg_offset_in_cutout,
    front_cutout_y + motor_cutout_l - peg_offset_in_cutout,
    rear_cutout_y  + peg_offset_in_cutout,
    rear_cutout_y  + motor_cutout_l - peg_offset_in_cutout
];

// Mast hole positions
mast_hole_x_positions = [
    mast_x0 + mast_hole_side_inset,
    mast_x0 + mast_w - mast_hole_side_inset
];
mast_hole_z = outer_h + mast_h - mast_hole_top_inset;


// Main chassis body with all cutouts applied
module chassis_body() {
    difference() {
        union() {
            // Outer box
            cube([outer_w, outer_l, outer_h], center = false);

            // Top walls for circuit board mount
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

        // Camera mast holes
        for (x_pos = mast_hole_x_positions) {
            translate([x_pos, mast_y0 - mast_hole_overlap, mast_hole_z])
                rotate([-90, 0, 0])
                    cylinder(
                        h = mast_l + 2 * mast_hole_overlap,
                        r = mast_hole_r,
                        $fn = 48
                    );
        }

        // Camera mast cutout: lower-right quarter when viewed from behind
        translate([
            mast_x0 - mast_cutout_overlap,
            mast_y0 - mast_cutout_overlap,
            outer_h - mast_cutout_overlap
        ])
        cube([
            mast_cutout_w + mast_cutout_overlap,
            mast_l + 2 * mast_cutout_overlap,
            mast_cutout_h + mast_cutout_overlap
        ], center = false);
    }
}


// Motor pegs added AFTER cutouts so they remain visible
module motor_pegs() {
    // Left side pegs: extend from x=5 back out to x=0 into the cutout
    for (y_pos = motor_peg_y_positions) {
        translate([motor_cutout_depth, y_pos, motor_peg_z])
            rotate([0, -90, 0])
                cylinder(h = motor_peg_len, r = motor_peg_r, $fn = 48);
    }

    // Right side pegs: extend from x=65 out to x=70 into the cutout
    for (y_pos = motor_peg_y_positions) {
        translate([outer_w - motor_cutout_depth, y_pos, motor_peg_z])
            rotate([0, 90, 0])
                cylinder(h = motor_peg_len, r = motor_peg_r, $fn = 48);
    }
}


// Final model
union() {
    chassis_body();
    motor_pegs();
}
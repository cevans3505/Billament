-- A database shaped like one saved by Billament before invoice prices were frozen (before 3.2).
-- The jobs table has no frozen price or finalized total columns, so opening it with Database runs the one-time freeze.
-- Built from the original CREATE TABLE statements in Database.EnsureCreated, minus the columns later versions add.
-- Sample names only.

CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE filaments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    vendor TEXT NOT NULL, material TEXT NOT NULL, color_name TEXT NOT NULL, color_hex TEXT NOT NULL,
    spool_price TEXT NOT NULL, spool_grams TEXT NOT NULL, active INTEGER NOT NULL, notes TEXT NOT NULL);
CREATE TABLE filament_mappings (key TEXT PRIMARY KEY, filament_id INTEGER NOT NULL);
CREATE TABLE jobs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL, customer TEXT NOT NULL, invoice_number TEXT, created_date TEXT NOT NULL,
    invoice_date TEXT, modeling_hours TEXT NOT NULL, post_processing INTEGER NOT NULL,
    paid INTEGER NOT NULL, paid_date TEXT, notes TEXT NOT NULL,
    printed INTEGER NOT NULL DEFAULT 0, printed_date TEXT,
    finalized INTEGER NOT NULL DEFAULT 0, finalized_date TEXT);
CREATE TABLE parts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    job_id INTEGER NOT NULL REFERENCES jobs(id) ON DELETE CASCADE,
    sort INTEGER NOT NULL, name TEXT NOT NULL, charge_slicing INTEGER NOT NULL);
-- Printers existed from 3.0, before snapshots in 3.2, so plates already carry the technology and printer columns.
CREATE TABLE plates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    part_id INTEGER NOT NULL REFERENCES parts(id) ON DELETE CASCADE,
    sort INTEGER NOT NULL, source_file TEXT NOT NULL, plate_index INTEGER NOT NULL, copies INTEGER NOT NULL,
    parts_per_plate INTEGER NOT NULL, sliced_seconds INTEGER NOT NULL, override_seconds INTEGER, thumbnail BLOB,
    technology INTEGER NOT NULL DEFAULT 0, printer_id INTEGER);
CREATE TABLE plate_filaments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    plate_id INTEGER NOT NULL REFERENCES plates(id) ON DELETE CASCADE,
    slot_id INTEGER NOT NULL, slicer_type TEXT NOT NULL, slicer_color_hex TEXT NOT NULL,
    slicer_vendor TEXT NOT NULL, slicer_profile TEXT NOT NULL, sliced_grams TEXT NOT NULL,
    override_grams TEXT, filament_id INTEGER, customer_supplied INTEGER NOT NULL);
CREATE TABLE printers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL, technology INTEGER NOT NULL, machine_rate TEXT NOT NULL, watts TEXT NOT NULL,
    waste TEXT NOT NULL, markup TEXT NOT NULL, active INTEGER NOT NULL, notes TEXT NOT NULL);

INSERT INTO settings(key, value) VALUES
    ('BusinessName', 'Sample Printing Co.'),
    ('ElectricityPerKwh', '0.20'),
    ('ModelingRatePerHour', '50'),
    ('SlicingFeePerPart', '10'),
    ('PostProcessingFee', '20');

INSERT INTO filaments(id, vendor, material, color_name, color_hex, spool_price, spool_grams, active, notes)
VALUES (1, 'Sample Filament Co.', 'PLA', 'Black', '#000000', '20.00', '1000', 1, '');

INSERT INTO printers(id, name, technology, machine_rate, watts, waste, markup, active, notes)
VALUES (1, 'Sample FDM printer', 0, '2.00', '500', '0.25', '0.20', 1, '');

-- Two finalized invoices (1 and 2) and one draft (3). Dates are fixed.
INSERT INTO jobs(id, title, customer, invoice_number, created_date, invoice_date, modeling_hours, post_processing,
                 paid, paid_date, notes, printed, printed_date, finalized, finalized_date)
VALUES
    (1, 'Dive light brackets', 'Harbor Dive Shop', '2025-014', '2025-11-03', '2025-11-05', '1', 1,
     1, '2025-11-20', '', 1, '2025-11-04', 1, '2025-11-05'),
    (2, 'Fin clips', 'Harbor Dive Shop', '2025-015', '2025-11-10', '2025-11-12', '0.5', 0,
     0, NULL, '', 1, '2025-11-11', 1, '2025-11-12'),
    (3, 'Mask strap keepers', 'Harbor Dive Shop', NULL, '2025-11-20', NULL, '0', 0,
     0, NULL, '', 0, NULL, 0, NULL);

INSERT INTO parts(id, job_id, sort, name, charge_slicing) VALUES
    (1, 1, 0, 'Bracket', 1),
    (2, 2, 0, 'Clip', 1),
    (3, 3, 0, 'Keeper', 1);

INSERT INTO plates(id, part_id, sort, source_file, plate_index, copies, parts_per_plate, sliced_seconds,
                   override_seconds, thumbnail, technology, printer_id) VALUES
    (1, 1, 0, 'brackets.gcode.3mf', 1, 2, 1, 3600, NULL, NULL, 0, 1),
    (2, 2, 0, 'clips.gcode.3mf', 1, 4, 1, 1800, NULL, NULL, 0, 1),
    (3, 3, 0, 'keepers.gcode.3mf', 1, 1, 1, 3600, NULL, NULL, 0, 1);

INSERT INTO plate_filaments(plate_id, slot_id, slicer_type, slicer_color_hex, slicer_vendor, slicer_profile,
                            sliced_grams, override_grams, filament_id, customer_supplied) VALUES
    (1, 1, 'PLA', '#000000', 'Sample Filament Co.', 'Sample PLA', '50', NULL, 1, 0),
    (2, 1, 'PLA', '#000000', 'Sample Filament Co.', 'Sample PLA', '10', NULL, 1, 0),
    (3, 1, 'PLA', '#000000', 'Sample Filament Co.', 'Sample PLA', '20', NULL, 1, 0);

-- PostgreSQL 16 init script for SqlBuilder Playground
-- Creates schema, seeds reference data, seeds transactional data

-- ─── SCHEMA ──────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS categories (
    id                 SERIAL         PRIMARY KEY,
    name               VARCHAR(150)   NOT NULL,
    slug               VARCHAR(150)   NOT NULL UNIQUE,
    parent_category_id INTEGER            REFERENCES categories(id) ON DELETE SET NULL,
    is_active          BOOLEAN        NOT NULL DEFAULT TRUE,
    sort_order         INTEGER        NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS customers (
    id         SERIAL        PRIMARY KEY,
    name       VARCHAR(250)  NOT NULL,
    email      VARCHAR(320)  NOT NULL UNIQUE,
    phone      VARCHAR(30),
    tax_id     VARCHAR(50),
    is_active  BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS products (
    id          SERIAL         PRIMARY KEY,
    category_id INTEGER        NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    name        VARCHAR(250)   NOT NULL,
    sku         VARCHAR(100)   NOT NULL UNIQUE,
    description TEXT,
    price       NUMERIC(18,4)  NOT NULL CHECK (price >= 0),
    cost_price  NUMERIC(18,4)  NOT NULL DEFAULT 0,
    stock       INTEGER        NOT NULL DEFAULT 0,
    min_stock   INTEGER        NOT NULL DEFAULT 0,
    is_active   BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS orders (
    id              SERIAL         PRIMARY KEY,
    customer_id     INTEGER        NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    status          VARCHAR(20)    NOT NULL DEFAULT 'pending'
                                   CHECK (status IN ('pending','confirmed','shipped','delivered','cancelled')),
    notes           TEXT,
    total_amount    NUMERIC(18,4)  NOT NULL DEFAULT 0,
    tax_amount      NUMERIC(18,4)  NOT NULL DEFAULT 0,
    discount_amount NUMERIC(18,4)  NOT NULL DEFAULT 0,
    currency        CHAR(3)        NOT NULL DEFAULT 'USD',
    created_at      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN        NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS order_items (
    id               SERIAL         PRIMARY KEY,
    order_id         INTEGER        NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id       INTEGER        NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    quantity         INTEGER        NOT NULL CHECK (quantity > 0),
    unit_price       NUMERIC(18,4)  NOT NULL DEFAULT 0,
    discount_percent NUMERIC(5,2)   NOT NULL DEFAULT 0,
    total_price      NUMERIC(18,4)  NOT NULL DEFAULT 0
);

-- ─── SEED: Categories ─────────────────────────────────────────────────────────

INSERT INTO categories (id, name, slug, is_active, sort_order) VALUES
    (1,  'Electronics',        'electronics',        TRUE, 1),
    (2,  'Computers',          'computers',          TRUE, 2),
    (3,  'Laptops',            'laptops',            TRUE, 3),
    (4,  'Smartphones',        'smartphones',        TRUE, 4),
    (5,  'Tablets',            'tablets',            TRUE, 5),
    (6,  'Audio',              'audio',              TRUE, 6),
    (7,  'Gaming',             'gaming',             TRUE, 7),
    (8,  'Cameras',            'cameras',            TRUE, 8),
    (9,  'Clothing',           'clothing',           TRUE, 9),
    (10, 'Home & Garden',      'home-garden',        TRUE, 10)
ON CONFLICT DO NOTHING;

-- ─── SEED: Customers (20 demo records) ────────────────────────────────────────

INSERT INTO customers (name, email, phone, is_active) VALUES
    ('Acme Corporation',    'billing@acme.corp',     '+1-555-0100', TRUE),
    ('Globex Industries',   'orders@globex.com',     '+1-555-0101', TRUE),
    ('Initech LLC',         'accounts@initech.com',  '+1-555-0102', TRUE),
    ('Umbrella Corp',       'orders@umbrella.net',   '+1-555-0103', TRUE),
    ('Stark Industries',    'tony@stark.io',          '+1-555-0104', TRUE),
    ('Wayne Enterprises',   'bruce@wayne.biz',        '+1-555-0105', TRUE),
    ('Oscorp Technologies', 'info@oscorp.tech',       '+1-555-0106', TRUE),
    ('LexCorp Global',      'lex@lexcorp.co',         '+1-555-0107', TRUE),
    ('Daily Planet Media',  'clark@dailyplanet.com',  '+1-555-0108', TRUE),
    ('S.H.I.E.L.D. HQ',    'fury@shield.gov',        '+1-555-0109', TRUE),
    ('Cyberdyne Systems',   'support@cyberdyne.ai',   '+1-555-0110', FALSE),
    ('Weyland-Yutani Corp', 'info@weylandyutani.com', '+1-555-0111', TRUE),
    ('Nakatomi Trading',    'billing@nakatomi.jp',    '+1-555-0112', TRUE),
    ('Massive Dynamic',     'peter@massivedynamic.io','+1-555-0113', TRUE),
    ('InGen Biotech',       'orders@ingen.bio',       '+1-555-0114', TRUE),
    ('Soylent Corp',        'info@soylent.co',        '+1-555-0115', FALSE),
    ('Buy More Electronics','buster@buymore.com',     '+1-555-0116', TRUE),
    ('Dunder Mifflin Inc',  'michael@dundermifflin.biz','+1-555-0117',TRUE),
    ('Pied Piper LLC',      'richard@piedpiper.com',  '+1-555-0118', TRUE),
    ('Hooli Corp',          'gavin@hooli.com',        '+1-555-0119', TRUE);

-- ─── SEED: Products (30 demo records) ────────────────────────────────────────

INSERT INTO products (category_id, name, sku, price, cost_price, stock, min_stock, is_active) VALUES
    (3,  'MacBook Pro 16" M4',        'LAPTOP-MBP16-M4',   3499.00, 2800.00, 25,  5, TRUE),
    (3,  'Dell XPS 15 OLED',          'LAPTOP-XPS15-OLED', 2199.00, 1700.00, 18,  5, TRUE),
    (3,  'ThinkPad X1 Carbon Gen 12', 'LAPTOP-TP-X1-G12',  1899.00, 1500.00, 30,  8, TRUE),
    (3,  'ASUS ROG Zephyrus G16',     'LAPTOP-ASUS-G16',   1599.00, 1200.00, 12,  3, TRUE),
    (4,  'iPhone 16 Pro Max 256GB',   'PHONE-IP16PM-256',  1199.00,  900.00, 50, 10, TRUE),
    (4,  'Samsung Galaxy S25 Ultra',  'PHONE-SGS25U',      1099.00,  820.00, 45, 10, TRUE),
    (4,  'Google Pixel 9 Pro XL',     'PHONE-GP9PXL',       999.00,  750.00, 35,  8, TRUE),
    (4,  'OnePlus 13 Pro',            'PHONE-OP13P',        699.00,  520.00, 22,  5, TRUE),
    (5,  'iPad Pro 13" M4 WiFi 512',  'TABLET-IPAD-P13M4', 1299.00, 1000.00, 20,  5, TRUE),
    (5,  'Samsung Galaxy Tab S10+',   'TABLET-SGS10P',      899.00,  670.00, 15,  4, TRUE),
    (6,  'Sony WH-1000XM6',           'AUDIO-WH1000XM6',   399.00,  290.00, 60, 15, TRUE),
    (6,  'Apple AirPods Pro 3',       'AUDIO-APP3',         249.00,  180.00, 80, 20, TRUE),
    (6,  'Bose QuietComfort 45',      'AUDIO-BQC45',        329.00,  240.00, 40, 10, TRUE),
    (6,  'Sennheiser HD 660S2',       'AUDIO-SHD660S2',     499.00,  370.00, 20,  5, TRUE),
    (7,  'PlayStation 5 Slim',        'GAMING-PS5S',        449.00,  380.00, 30,  8, TRUE),
    (7,  'Xbox Series X',             'GAMING-XBSX',        499.00,  420.00, 25,  6, TRUE),
    (7,  'Nintendo Switch OLED',      'GAMING-NSWITCH',     349.00,  270.00, 35,  8, TRUE),
    (7,  'Steam Deck OLED 512GB',     'GAMING-SDOLED512',   549.00,  420.00, 18,  5, TRUE),
    (8,  'Sony A7 V Mirrorless',      'CAMERA-SNYA7V',     3299.00, 2600.00,  8,  2, TRUE),
    (8,  'Canon EOS R6 Mark III',     'CAMERA-EOSR6M3',    2799.00, 2200.00, 10,  3, TRUE),
    (1,  'Anker 200W GaN Charger',    'ACC-ANKER200W',       89.99,   55.00,100, 25, TRUE),
    (1,  'Belkin 4-Port USB-C Hub',   'ACC-BELK4CH',         59.99,   35.00, 80, 20, TRUE),
    (1,  'Samsung T9 4TB SSD',        'STORAGE-SAM-T9-4T',  379.00,  280.00, 40, 10, TRUE),
    (1,  'WD My Cloud EX4100',        'STORAGE-WD-EX41',    649.00,  490.00, 15,  4, TRUE),
    (2,  'Apple Mac mini M4 Pro',     'DESKTOP-MMINI-M4P', 1399.00, 1100.00, 20,  5, TRUE),
    (2,  'Intel NUC 14 Extreme',      'DESKTOP-NUC14E',    1899.00, 1500.00, 10,  3, TRUE),
    (3,  'HP Spectre x360 16',        'LAPTOP-HPSX360-16', 1799.00, 1400.00, 14,  4, TRUE),
    (4,  'Sony Xperia 1 VII',         'PHONE-SXP1VII',      999.00,  750.00, 20,  5, TRUE),
    (6,  'JBL Charge 6',              'AUDIO-JBLC6',        199.00,  140.00, 55, 12, TRUE),
    (7,  'Razer Blade 18',            'GAMING-RZR18',      3499.00, 2800.00,  6,  2, TRUE);

-- ─── SEED: Orders (50 demo records) ──────────────────────────────────────────

INSERT INTO orders (customer_id, status, total_amount, tax_amount, currency) VALUES
    (1,  'delivered',  4698.00, 375.84, 'USD'),
    (1,  'shipped',    3748.00, 299.84, 'USD'),
    (2,  'confirmed',  1248.00,  99.84, 'USD'),
    (2,  'pending',     449.00,  35.92, 'USD'),
    (3,  'delivered',  2648.00, 211.84, 'USD'),
    (4,  'cancelled',     0.00,   0.00, 'USD'),
    (5,  'delivered',  5998.00, 479.84, 'USD'),
    (6,  'shipped',    2198.00, 175.84, 'USD'),
    (7,  'confirmed',  1648.00, 131.84, 'USD'),
    (8,  'pending',     898.00,  71.84, 'USD'),
    (9,  'delivered',  3498.00, 279.84, 'USD'),
    (10, 'shipped',    4198.00, 335.84, 'USD'),
    (11, 'cancelled',     0.00,   0.00, 'USD'),
    (12, 'delivered',  1898.00, 151.84, 'USD'),
    (13, 'confirmed',  2548.00, 203.84, 'USD'),
    (14, 'pending',    1099.00,  87.92, 'USD'),
    (15, 'delivered',  3998.00, 319.84, 'USD'),
    (16, 'shipped',     699.00,  55.92, 'USD'),
    (17, 'confirmed',  1399.00, 111.92, 'USD'),
    (18, 'delivered',  5598.00, 447.84, 'USD'),
    (19, 'pending',     849.00,  67.92, 'USD'),
    (20, 'delivered',  2998.00, 239.84, 'USD'),
    (1,  'confirmed',  1299.00, 103.92, 'USD'),
    (3,  'shipped',    3699.00, 295.92, 'USD'),
    (5,  'delivered',  4998.00, 399.84, 'USD'),
    (7,  'pending',     349.00,  27.92, 'USD'),
    (9,  'confirmed',  2199.00, 175.92, 'USD'),
    (11, 'delivered',  1499.00, 119.92, 'USD'),
    (13, 'shipped',    3399.00, 271.92, 'USD'),
    (15, 'pending',     549.00,  43.92, 'USD'),
    (2,  'delivered',  4798.00, 383.84, 'USD'),
    (4,  'confirmed',  1998.00, 159.84, 'USD'),
    (6,  'shipped',    2798.00, 223.84, 'USD'),
    (8,  'delivered',  3998.00, 319.84, 'USD'),
    (10, 'pending',    1198.00,  95.84, 'USD'),
    (12, 'confirmed',  2498.00, 199.84, 'USD'),
    (14, 'delivered',  4298.00, 343.84, 'USD'),
    (16, 'shipped',    1698.00, 135.84, 'USD'),
    (18, 'pending',     799.00,  63.92, 'USD'),
    (20, 'confirmed',  3198.00, 255.84, 'USD'),
    (1,  'delivered',  6598.00, 527.84, 'USD'),
    (3,  'shipped',    2098.00, 167.84, 'USD'),
    (5,  'confirmed',  1498.00, 119.84, 'USD'),
    (7,  'delivered',  3798.00, 303.84, 'USD'),
    (9,  'pending',     649.00,  51.92, 'USD'),
    (11, 'confirmed',  2298.00, 183.84, 'USD'),
    (13, 'delivered',  4898.00, 391.84, 'USD'),
    (15, 'shipped',    1298.00, 103.84, 'USD'),
    (17, 'pending',     499.00,  39.92, 'USD'),
    (19, 'delivered',  3698.00, 295.84, 'USD');

-- ─── SEED: Order Items ────────────────────────────────────────────────────────

-- Order 1: MacBook Pro + AirPods
INSERT INTO order_items (order_id, product_id, quantity, unit_price, total_price) VALUES
    (1, 1,  1, 3499.00, 3499.00),
    (1, 12, 1,  249.00,  249.00),
    (1, 22, 2,   59.99,  119.98);

-- Order 2: Dell XPS + Sony WH
INSERT INTO order_items (order_id, product_id, quantity, unit_price, total_price) VALUES
    (2, 2,  1, 2199.00, 2199.00),
    (2, 11, 1,  399.00,  399.00),
    (2, 21, 1,   89.99,   89.99);

-- Order 3: iPad + AirPods
INSERT INTO order_items (order_id, product_id, quantity, unit_price, total_price) VALUES
    (3, 9,  1,  999.00,  999.00),
    (3, 12, 1,  249.00,  249.00);

-- Order 5: MacBook + iPhone
INSERT INTO order_items (order_id, product_id, quantity, unit_price, total_price) VALUES
    (5, 1, 1, 3499.00, 3499.00),
    (5, 5, 2, 1199.00, 2398.00);

ANALYZE;

-- =============================================================================
-- EricksonLopez.SqlBuilder — PostgreSQL DDL
-- Version: 1.0.0 | Target: PostgreSQL 14+
-- =============================================================================
-- Notes:
--   • Uses GENERATED ALWAYS AS IDENTITY (SQL:2003 standard, preferred over SERIAL)
--   • TEXT for variable-length strings (no artificial length limits)
--   • TIMESTAMPTZ (timestamp with time zone) for all datetime columns
--   • JSONB for JSON storage (binary, indexed, preferred over JSON)
--   • BOOLEAN native type
--   • UUID via gen_random_uuid() (pgcrypto or built-in pg 13+)
--   • BIGINT for audit_logs.id (high-volume table)
-- =============================================================================

-- ─── Extensions ──────────────────────────────────────────────────────────────
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ─── Drop existing tables (dev/test convenience) ─────────────────────────────
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS user_roles CASCADE;
DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS invoices CASCADE;
DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS products CASCADE;
DROP TABLE IF EXISTS categories CASCADE;
DROP TABLE IF EXISTS addresses CASCADE;
DROP TABLE IF EXISTS customers CASCADE;
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS users CASCADE;

-- ─── Customers ────────────────────────────────────────────────────────────────
CREATE TABLE customers (
    id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL,
    email       TEXT NOT NULL UNIQUE,
    phone       TEXT,
    tax_id      TEXT,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ
);

CREATE INDEX idx_customers_email ON customers(email);
CREATE INDEX idx_customers_is_active ON customers(is_active);
CREATE INDEX idx_customers_created_at ON customers(created_at DESC);

COMMENT ON TABLE customers IS 'Business and individual customers';
COMMENT ON COLUMN customers.tax_id IS 'Tax identification number (RFC, EIN, VAT, etc.)';

-- ─── Addresses ────────────────────────────────────────────────────────────────
CREATE TABLE addresses (
    id           INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id  INTEGER NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    street       TEXT NOT NULL,
    city         TEXT NOT NULL,
    state        TEXT NOT NULL,
    country      TEXT NOT NULL DEFAULT 'US',
    postal_code  TEXT NOT NULL,
    address_type TEXT NOT NULL DEFAULT 'shipping' CHECK (address_type IN ('shipping', 'billing')),
    is_default   BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_addresses_customer_id ON addresses(customer_id);

-- ─── Categories ───────────────────────────────────────────────────────────────
CREATE TABLE categories (
    id                 INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name               TEXT NOT NULL,
    slug               TEXT NOT NULL UNIQUE,
    parent_category_id INTEGER REFERENCES categories(id) ON DELETE SET NULL,
    is_active          BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order         INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_categories_parent_id ON categories(parent_category_id);
CREATE INDEX idx_categories_slug ON categories(slug);

-- ─── Products ─────────────────────────────────────────────────────────────────
CREATE TABLE products (
    id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_id INTEGER NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
    name        TEXT NOT NULL,
    sku         TEXT NOT NULL UNIQUE,
    description TEXT,
    price       NUMERIC(18,4) NOT NULL CHECK (price >= 0),
    cost_price  NUMERIC(18,4) NOT NULL DEFAULT 0 CHECK (cost_price >= 0),
    stock       INTEGER NOT NULL DEFAULT 0,
    min_stock   INTEGER NOT NULL DEFAULT 0,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ
);

CREATE INDEX idx_products_category_id ON products(category_id);
CREATE INDEX idx_products_sku ON products(sku);
CREATE INDEX idx_products_is_active ON products(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_products_price ON products(price);

-- ─── Orders ───────────────────────────────────────────────────────────────────
CREATE TABLE orders (
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id     INTEGER NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
    status          TEXT NOT NULL DEFAULT 'pending'
                    CHECK (status IN ('pending','confirmed','shipped','delivered','cancelled')),
    notes           TEXT,
    total_amount    NUMERIC(18,4) NOT NULL CHECK (total_amount >= 0),
    tax_amount      NUMERIC(18,4) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(18,4) NOT NULL DEFAULT 0,
    currency        CHAR(3) NOT NULL DEFAULT 'USD',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    confirmed_at    TIMESTAMPTZ,
    shipped_at      TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ
);

CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at DESC);
CREATE INDEX idx_orders_not_deleted ON orders(customer_id) WHERE is_deleted = FALSE;

-- ─── Order Items ──────────────────────────────────────────────────────────────
CREATE TABLE order_items (
    id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id         INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id       INTEGER NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    quantity         INTEGER NOT NULL CHECK (quantity > 0),
    unit_price       NUMERIC(18,4) NOT NULL CHECK (unit_price >= 0),
    discount_percent NUMERIC(5,2) NOT NULL DEFAULT 0 CHECK (discount_percent BETWEEN 0 AND 100),
    total_price      NUMERIC(18,4) NOT NULL CHECK (total_price >= 0),
    notes            TEXT
);

CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- ─── Invoices ─────────────────────────────────────────────────────────────────
CREATE TABLE invoices (
    id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id         INTEGER NOT NULL REFERENCES orders(id) ON DELETE RESTRICT,
    invoice_number   TEXT NOT NULL UNIQUE,
    status           TEXT NOT NULL DEFAULT 'draft'
                     CHECK (status IN ('draft','issued','paid','overdue','cancelled')),
    subtotal_amount  NUMERIC(18,4) NOT NULL,
    tax_amount       NUMERIC(18,4) NOT NULL DEFAULT 0,
    total_amount     NUMERIC(18,4) NOT NULL,
    paid_amount      NUMERIC(18,4) NOT NULL DEFAULT 0,
    currency         CHAR(3) NOT NULL DEFAULT 'USD',
    issued_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    due_at           TIMESTAMPTZ NOT NULL,
    paid_at          TIMESTAMPTZ,
    notes            TEXT
);

CREATE INDEX idx_invoices_order_id ON invoices(order_id);
CREATE INDEX idx_invoices_status ON invoices(status);
CREATE INDEX idx_invoices_due_at ON invoices(due_at) WHERE status NOT IN ('paid','cancelled');

-- ─── Payments ─────────────────────────────────────────────────────────────────
CREATE TABLE payments (
    id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    invoice_id       INTEGER NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
    amount           NUMERIC(18,4) NOT NULL CHECK (amount > 0),
    method           TEXT NOT NULL CHECK (method IN ('credit_card','bank_transfer','paypal','stripe','cash','check')),
    status           TEXT NOT NULL DEFAULT 'pending'
                     CHECK (status IN ('pending','completed','failed','refunded')),
    transaction_ref  TEXT,
    gateway_response TEXT,
    paid_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refunded_at      TIMESTAMPTZ,
    refunded_amount  NUMERIC(18,4)
);

CREATE INDEX idx_payments_invoice_id ON payments(invoice_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_transaction_ref ON payments(transaction_ref) WHERE transaction_ref IS NOT NULL;

-- ─── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE users (
    id                    INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username              TEXT NOT NULL UNIQUE,
    email                 TEXT NOT NULL UNIQUE,
    password_hash         TEXT NOT NULL,
    first_name            TEXT,
    last_name             TEXT,
    is_active             BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at         TIMESTAMPTZ,
    locked_until          TIMESTAMPTZ,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_is_active ON users(is_active) WHERE is_active = TRUE;

-- ─── Roles ────────────────────────────────────────────────────────────────────
CREATE TABLE roles (
    id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    description TEXT,
    permissions JSONB NOT NULL DEFAULT '[]'::jsonb,
    is_system   BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_roles_name ON roles(name);
CREATE INDEX idx_roles_permissions ON roles USING gin(permissions);

-- ─── User Roles (junction) ────────────────────────────────────────────────────
CREATE TABLE user_roles (
    user_id            INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id            INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_by_user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);

-- ─── Audit Logs ───────────────────────────────────────────────────────────────
CREATE TABLE audit_logs (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    entity_name     TEXT NOT NULL,
    entity_id       TEXT NOT NULL,
    action          TEXT NOT NULL CHECK (action IN ('INSERT','UPDATE','DELETE')),
    old_values      JSONB,
    new_values      JSONB,
    changed_fields  JSONB,
    user_id         INTEGER REFERENCES users(id) ON DELETE SET NULL,
    ip_address      TEXT,
    user_agent      TEXT,
    timestamp       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    correlation_id  TEXT
) PARTITION BY RANGE (timestamp);

-- Partition by month (recommended for high-volume audit tables)
CREATE TABLE audit_logs_2026_01 PARTITION OF audit_logs
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
CREATE TABLE audit_logs_2026_07 PARTITION OF audit_logs
    FOR VALUES FROM ('2026-07-01') TO ('2026-08-01');
CREATE TABLE audit_logs_default PARTITION OF audit_logs DEFAULT;

CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_name, entity_id);
CREATE INDEX idx_audit_logs_timestamp ON audit_logs(timestamp DESC);
CREATE INDEX idx_audit_logs_user_id ON audit_logs(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_audit_logs_action ON audit_logs(action);

-- ─── Seed: Roles ─────────────────────────────────────────────────────────────
INSERT INTO roles (name, description, permissions, is_system) VALUES
    ('admin',   'Full system administrator', '["*"]'::jsonb, TRUE),
    ('manager', 'Can manage orders, products, customers', '["orders:*","products:*","customers:*"]'::jsonb, TRUE),
    ('viewer',  'Read-only access', '["orders:read","products:read","customers:read"]'::jsonb, TRUE),
    ('support', 'Customer support role', '["orders:read","customers:*"]'::jsonb, FALSE);

-- ─── Seed: Categories ─────────────────────────────────────────────────────────
INSERT INTO categories (name, slug, parent_category_id, sort_order) VALUES
    ('Electronics',         'electronics',          NULL, 1),
    ('Clothing & Apparel',  'clothing',             NULL, 2),
    ('Home & Garden',       'home-garden',          NULL, 3),
    ('Books & Media',       'books-media',          NULL, 4),
    ('Laptops',             'laptops',              1,    1),
    ('Smartphones',         'smartphones',          1,    2),
    ('Accessories',         'accessories',          1,    3),
    ('Men''s Clothing',     'mens-clothing',        2,    1),
    ('Women''s Clothing',   'womens-clothing',      2,    2),
    ('Gaming Laptops',      'gaming-laptops',       5,    1);

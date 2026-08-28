-- =============================================================================
-- EricksonLopez.SqlBuilder — SQL Server DDL
-- Version: 1.0.0 | Target: SQL Server 2019+ / Azure SQL
-- =============================================================================
-- Notes:
--   • Uses IDENTITY(1,1) for auto-increment (classic) or SEQUENCE objects
--   • NVARCHAR for Unicode strings, NVARCHAR(MAX) for long text
--   • DATETIMEOFFSET for timezone-aware datetimes
--   • NVARCHAR(MAX) with ISJSON() check constraint for JSON storage
--   • BIT for boolean values (0/1)
--   • UNIQUEIDENTIFIER for UUIDs (optional, here we use INT for simplicity)
--   • Schema: dbo (default)
--   • NEWID() for UUIDs, GETUTCDATE() for UTC timestamps
--   • OUTPUT clause replaces RETURNING
-- =============================================================================

USE master;
GO

-- Create database if not in a test environment
-- CREATE DATABASE SqlBuilderDemo;
-- GO
-- USE SqlBuilderDemo;
-- GO

-- ─── Drop existing tables ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.audit_logs',   'U') IS NOT NULL DROP TABLE dbo.audit_logs;
IF OBJECT_ID('dbo.user_roles',   'U') IS NOT NULL DROP TABLE dbo.user_roles;
IF OBJECT_ID('dbo.payments',     'U') IS NOT NULL DROP TABLE dbo.payments;
IF OBJECT_ID('dbo.invoices',     'U') IS NOT NULL DROP TABLE dbo.invoices;
IF OBJECT_ID('dbo.order_items',  'U') IS NOT NULL DROP TABLE dbo.order_items;
IF OBJECT_ID('dbo.orders',       'U') IS NOT NULL DROP TABLE dbo.orders;
IF OBJECT_ID('dbo.products',     'U') IS NOT NULL DROP TABLE dbo.products;
IF OBJECT_ID('dbo.categories',   'U') IS NOT NULL DROP TABLE dbo.categories;
IF OBJECT_ID('dbo.addresses',    'U') IS NOT NULL DROP TABLE dbo.addresses;
IF OBJECT_ID('dbo.customers',    'U') IS NOT NULL DROP TABLE dbo.customers;
IF OBJECT_ID('dbo.roles',        'U') IS NOT NULL DROP TABLE dbo.roles;
IF OBJECT_ID('dbo.users',        'U') IS NOT NULL DROP TABLE dbo.users;
GO

-- ─── Customers ────────────────────────────────────────────────────────────────
CREATE TABLE dbo.customers (
    id          INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(255) NOT NULL,
    email       NVARCHAR(320) NOT NULL,
    phone       NVARCHAR(50),
    tax_id      NVARCHAR(50),
    is_active   BIT           NOT NULL DEFAULT 1,
    created_at  DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at  DATETIMEOFFSET,
    CONSTRAINT uq_customers_email UNIQUE (email)
);

CREATE NONCLUSTERED INDEX idx_customers_email     ON dbo.customers(email);
CREATE NONCLUSTERED INDEX idx_customers_is_active ON dbo.customers(is_active);
CREATE NONCLUSTERED INDEX idx_customers_created   ON dbo.customers(created_at DESC);
GO

-- ─── Addresses ────────────────────────────────────────────────────────────────
CREATE TABLE dbo.addresses (
    id           INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    customer_id  INT           NOT NULL REFERENCES dbo.customers(id) ON DELETE CASCADE,
    street       NVARCHAR(500) NOT NULL,
    city         NVARCHAR(100) NOT NULL,
    state        NVARCHAR(100) NOT NULL,
    country      NVARCHAR(100) NOT NULL DEFAULT N'US',
    postal_code  NVARCHAR(20)  NOT NULL,
    address_type NVARCHAR(20)  NOT NULL DEFAULT N'shipping'
                 CONSTRAINT chk_address_type CHECK (address_type IN (N'shipping', N'billing')),
    is_default   BIT           NOT NULL DEFAULT 0
);

CREATE NONCLUSTERED INDEX idx_addresses_customer_id ON dbo.addresses(customer_id);
GO

-- ─── Categories ───────────────────────────────────────────────────────────────
CREATE TABLE dbo.categories (
    id                 INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name               NVARCHAR(200) NOT NULL,
    slug               NVARCHAR(200) NOT NULL,
    parent_category_id INT           REFERENCES dbo.categories(id) ON DELETE NO ACTION,
    is_active          BIT           NOT NULL DEFAULT 1,
    sort_order         INT           NOT NULL DEFAULT 0,
    CONSTRAINT uq_categories_slug UNIQUE (slug)
);

CREATE NONCLUSTERED INDEX idx_categories_parent_id ON dbo.categories(parent_category_id);
CREATE NONCLUSTERED INDEX idx_categories_slug      ON dbo.categories(slug);
GO

-- ─── Products ─────────────────────────────────────────────────────────────────
CREATE TABLE dbo.products (
    id          INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    category_id INT            NOT NULL REFERENCES dbo.categories(id) ON DELETE NO ACTION,
    name        NVARCHAR(400)  NOT NULL,
    sku         NVARCHAR(100)  NOT NULL,
    description NVARCHAR(MAX),
    price       DECIMAL(18,4)  NOT NULL CONSTRAINT chk_product_price CHECK (price >= 0),
    cost_price  DECIMAL(18,4)  NOT NULL DEFAULT 0 CONSTRAINT chk_cost_price CHECK (cost_price >= 0),
    stock       INT            NOT NULL DEFAULT 0,
    min_stock   INT            NOT NULL DEFAULT 0,
    is_active   BIT            NOT NULL DEFAULT 1,
    created_at  DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at  DATETIMEOFFSET,
    CONSTRAINT uq_products_sku UNIQUE (sku)
);

CREATE NONCLUSTERED INDEX idx_products_category_id ON dbo.products(category_id);
CREATE NONCLUSTERED INDEX idx_products_sku         ON dbo.products(sku);
CREATE NONCLUSTERED INDEX idx_products_is_active   ON dbo.products(is_active) WHERE is_active = 1;
CREATE NONCLUSTERED INDEX idx_products_price       ON dbo.products(price);
GO

-- ─── Orders ───────────────────────────────────────────────────────────────────
CREATE TABLE dbo.orders (
    id              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    customer_id     INT            NOT NULL REFERENCES dbo.customers(id) ON DELETE NO ACTION,
    status          NVARCHAR(20)   NOT NULL DEFAULT N'pending'
                    CONSTRAINT chk_order_status CHECK (status IN (N'pending',N'confirmed',N'shipped',N'delivered',N'cancelled')),
    notes           NVARCHAR(MAX),
    total_amount    DECIMAL(18,4)  NOT NULL CONSTRAINT chk_order_total CHECK (total_amount >= 0),
    tax_amount      DECIMAL(18,4)  NOT NULL DEFAULT 0,
    discount_amount DECIMAL(18,4)  NOT NULL DEFAULT 0,
    currency        NCHAR(3)       NOT NULL DEFAULT N'USD',
    created_at      DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    confirmed_at    DATETIMEOFFSET,
    shipped_at      DATETIMEOFFSET,
    delivered_at    DATETIMEOFFSET,
    is_deleted      BIT            NOT NULL DEFAULT 0,
    deleted_at      DATETIMEOFFSET
);

CREATE NONCLUSTERED INDEX idx_orders_customer_id  ON dbo.orders(customer_id);
CREATE NONCLUSTERED INDEX idx_orders_status       ON dbo.orders(status);
CREATE NONCLUSTERED INDEX idx_orders_created_at   ON dbo.orders(created_at DESC);
CREATE NONCLUSTERED INDEX idx_orders_not_deleted  ON dbo.orders(customer_id, created_at DESC) WHERE is_deleted = 0;
GO

-- ─── Order Items ──────────────────────────────────────────────────────────────
CREATE TABLE dbo.order_items (
    id               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    order_id         INT           NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
    product_id       INT           NOT NULL REFERENCES dbo.products(id) ON DELETE NO ACTION,
    quantity         INT           NOT NULL CONSTRAINT chk_oi_qty CHECK (quantity > 0),
    unit_price       DECIMAL(18,4) NOT NULL CONSTRAINT chk_oi_price CHECK (unit_price >= 0),
    discount_percent DECIMAL(5,2)  NOT NULL DEFAULT 0 CONSTRAINT chk_oi_disc CHECK (discount_percent BETWEEN 0 AND 100),
    total_price      DECIMAL(18,4) NOT NULL CONSTRAINT chk_oi_total CHECK (total_price >= 0),
    notes            NVARCHAR(MAX)
);

CREATE NONCLUSTERED INDEX idx_order_items_order_id   ON dbo.order_items(order_id);
CREATE NONCLUSTERED INDEX idx_order_items_product_id ON dbo.order_items(product_id);
GO

-- ─── Invoices ─────────────────────────────────────────────────────────────────
CREATE TABLE dbo.invoices (
    id              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    order_id        INT            NOT NULL REFERENCES dbo.orders(id) ON DELETE NO ACTION,
    invoice_number  NVARCHAR(100)  NOT NULL,
    status          NVARCHAR(20)   NOT NULL DEFAULT N'draft'
                    CONSTRAINT chk_inv_status CHECK (status IN (N'draft',N'issued',N'paid',N'overdue',N'cancelled')),
    subtotal_amount DECIMAL(18,4)  NOT NULL,
    tax_amount      DECIMAL(18,4)  NOT NULL DEFAULT 0,
    total_amount    DECIMAL(18,4)  NOT NULL,
    paid_amount     DECIMAL(18,4)  NOT NULL DEFAULT 0,
    currency        NCHAR(3)       NOT NULL DEFAULT N'USD',
    issued_at       DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    due_at          DATETIMEOFFSET NOT NULL,
    paid_at         DATETIMEOFFSET,
    notes           NVARCHAR(MAX),
    CONSTRAINT uq_invoices_number UNIQUE (invoice_number)
);

CREATE NONCLUSTERED INDEX idx_invoices_order_id ON dbo.invoices(order_id);
CREATE NONCLUSTERED INDEX idx_invoices_status   ON dbo.invoices(status);
CREATE NONCLUSTERED INDEX idx_invoices_due_at   ON dbo.invoices(due_at) WHERE status NOT IN (N'paid', N'cancelled');
GO

-- ─── Payments ─────────────────────────────────────────────────────────────────
CREATE TABLE dbo.payments (
    id               INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    invoice_id       INT            NOT NULL REFERENCES dbo.invoices(id) ON DELETE NO ACTION,
    amount           DECIMAL(18,4)  NOT NULL CONSTRAINT chk_pay_amount CHECK (amount > 0),
    method           NVARCHAR(30)   NOT NULL CONSTRAINT chk_pay_method CHECK (method IN (N'credit_card',N'bank_transfer',N'paypal',N'stripe',N'cash',N'check')),
    status           NVARCHAR(20)   NOT NULL DEFAULT N'pending'
                     CONSTRAINT chk_pay_status CHECK (status IN (N'pending',N'completed',N'failed',N'refunded')),
    transaction_ref  NVARCHAR(200),
    gateway_response NVARCHAR(MAX),
    paid_at          DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    refunded_at      DATETIMEOFFSET,
    refunded_amount  DECIMAL(18,4)
);

CREATE NONCLUSTERED INDEX idx_payments_invoice_id      ON dbo.payments(invoice_id);
CREATE NONCLUSTERED INDEX idx_payments_status          ON dbo.payments(status);
CREATE NONCLUSTERED INDEX idx_payments_transaction_ref ON dbo.payments(transaction_ref) WHERE transaction_ref IS NOT NULL;
GO

-- ─── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE dbo.users (
    id                    INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    username              NVARCHAR(100)  NOT NULL,
    email                 NVARCHAR(320)  NOT NULL,
    password_hash         NVARCHAR(500)  NOT NULL,
    first_name            NVARCHAR(100),
    last_name             NVARCHAR(100),
    is_active             BIT            NOT NULL DEFAULT 1,
    email_verified        BIT            NOT NULL DEFAULT 0,
    created_at            DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    last_login_at         DATETIMEOFFSET,
    locked_until          DATETIMEOFFSET,
    failed_login_attempts INT            NOT NULL DEFAULT 0,
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT uq_users_email    UNIQUE (email)
);

CREATE NONCLUSTERED INDEX idx_users_username  ON dbo.users(username);
CREATE NONCLUSTERED INDEX idx_users_email     ON dbo.users(email);
CREATE NONCLUSTERED INDEX idx_users_is_active ON dbo.users(is_active) WHERE is_active = 1;
GO

-- ─── Roles ────────────────────────────────────────────────────────────────────
CREATE TABLE dbo.roles (
    id          INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100)  NOT NULL,
    description NVARCHAR(500),
    permissions NVARCHAR(MAX)  NOT NULL DEFAULT N'[]'
                CONSTRAINT chk_roles_permissions_json CHECK (ISJSON(permissions) = 1),
    is_system   BIT            NOT NULL DEFAULT 0,
    created_at  DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT uq_roles_name UNIQUE (name)
);
GO

-- ─── User Roles ───────────────────────────────────────────────────────────────
CREATE TABLE dbo.user_roles (
    user_id             INT            NOT NULL REFERENCES dbo.users(id) ON DELETE CASCADE,
    role_id             INT            NOT NULL REFERENCES dbo.roles(id) ON DELETE CASCADE,
    assigned_at         DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    assigned_by_user_id INT            REFERENCES dbo.users(id) ON DELETE NO ACTION,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id)
);

CREATE NONCLUSTERED INDEX idx_user_roles_user_id ON dbo.user_roles(user_id);
CREATE NONCLUSTERED INDEX idx_user_roles_role_id ON dbo.user_roles(role_id);
GO

-- ─── Audit Logs ───────────────────────────────────────────────────────────────
-- SQL Server: Use table partitioning on timestamp via partition function/scheme
-- For simplicity in dev: single table with filtered index
CREATE TABLE dbo.audit_logs (
    id              BIGINT         NOT NULL IDENTITY(1,1) PRIMARY KEY,
    entity_name     NVARCHAR(100)  NOT NULL,
    entity_id       NVARCHAR(100)  NOT NULL,
    action          NVARCHAR(10)   NOT NULL CONSTRAINT chk_audit_action CHECK (action IN (N'INSERT',N'UPDATE',N'DELETE')),
    old_values      NVARCHAR(MAX),   -- JSON
    new_values      NVARCHAR(MAX),   -- JSON
    changed_fields  NVARCHAR(MAX),   -- JSON
    user_id         INT            REFERENCES dbo.users(id) ON DELETE SET NULL,
    ip_address      NVARCHAR(45),
    user_agent      NVARCHAR(500),
    timestamp       DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
    correlation_id  NVARCHAR(200)
);

CREATE NONCLUSTERED INDEX idx_audit_entity    ON dbo.audit_logs(entity_name, entity_id);
CREATE NONCLUSTERED INDEX idx_audit_timestamp ON dbo.audit_logs(timestamp DESC);
CREATE NONCLUSTERED INDEX idx_audit_user_id   ON dbo.audit_logs(user_id) WHERE user_id IS NOT NULL;
CREATE NONCLUSTERED INDEX idx_audit_action    ON dbo.audit_logs(action);
GO

-- ─── Seed Data ────────────────────────────────────────────────────────────────
INSERT INTO dbo.roles (name, description, permissions, is_system) VALUES
    (N'admin',   N'Full system administrator', N'["*"]', 1),
    (N'manager', N'Can manage orders, products, customers', N'["orders:*","products:*","customers:*"]', 1),
    (N'viewer',  N'Read-only access', N'["orders:read","products:read","customers:read"]', 1),
    (N'support', N'Customer support role', N'["orders:read","customers:*"]', 0);

INSERT INTO dbo.categories (name, slug, parent_category_id, sort_order) VALUES
    (N'Electronics',        N'electronics',   NULL, 1),
    (N'Clothing & Apparel', N'clothing',      NULL, 2),
    (N'Home & Garden',      N'home-garden',   NULL, 3),
    (N'Books & Media',      N'books-media',   NULL, 4),
    (N'Laptops',            N'laptops',       1,    1),
    (N'Smartphones',        N'smartphones',   1,    2),
    (N'Accessories',        N'accessories',   1,    3),
    (N'Men''s Clothing',    N'mens-clothing', 2,    1),
    (N'Women''s Clothing',  N'womens-clothing',2,   2),
    (N'Gaming Laptops',     N'gaming-laptops',5,    1);
GO

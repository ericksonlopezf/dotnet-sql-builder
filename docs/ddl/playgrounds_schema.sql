-- ╔══════════════════════════════════════════════════════════════════════════════╗
-- ║  EricksonLopez.SqlBuilder — Unified Playgrounds Schema                       ║
-- ║  Compatible with PostgreSQL, SQL Server, MySQL, SQLite, Oracle (Mostly)      ║
-- ╚══════════════════════════════════════════════════════════════════════════════╝

-- Note: In a real-world scenario, you would separate this by dialect. 
-- For illustrative documentation purposes, this presents the ANSI / Common subset.

CREATE TABLE categories (
    id        INT PRIMARY KEY, -- Use IDENTITY / AUTO_INCREMENT / SERIAL natively
    name      VARCHAR(150) NOT NULL,
    slug      VARCHAR(150) NOT NULL,
    is_active SMALLINT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_cat_slug UNIQUE (slug)
);

CREATE TABLE customers (
    id         INT PRIMARY KEY,
    name       VARCHAR(250) NOT NULL,
    email      VARCHAR(320) NOT NULL,
    phone      VARCHAR(30),
    is_active  SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL,
    CONSTRAINT UQ_cust_email UNIQUE (email)
);

CREATE TABLE products (
    id          INT PRIMARY KEY,
    category_id INT NOT NULL,
    name        VARCHAR(250) NOT NULL,
    sku         VARCHAR(100) NOT NULL,
    price       DECIMAL(18,4) NOT NULL,
    cost_price  DECIMAL(18,4) NOT NULL DEFAULT 0,
    stock       INT NOT NULL DEFAULT 0,
    is_active   SMALLINT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_prod_sku UNIQUE (sku)
);

CREATE TABLE orders (
    id           INT PRIMARY KEY,
    customer_id  INT NOT NULL,
    status       VARCHAR(20) NOT NULL DEFAULT 'pending',
    total_amount DECIMAL(18,4) NOT NULL DEFAULT 0,
    currency     CHAR(3) NOT NULL DEFAULT 'USD',
    is_deleted   SMALLINT NOT NULL DEFAULT 0,
    created_at   TIMESTAMP NOT NULL
);
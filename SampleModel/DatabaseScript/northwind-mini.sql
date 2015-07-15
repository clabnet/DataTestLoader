--
-- PostgreSQL database dump
--

-- Dumped from database version 9.4.1
-- Dumped by pg_dump version 9.4.1
-- Started on 2015-07-13 22:07:52

SET statement_timeout = 0;
SET lock_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET client_min_messages = warning;

--
-- TOC entry 175 (class 3079 OID 11855)
-- Name: plpgsql; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS plpgsql WITH SCHEMA pg_catalog;


--
-- TOC entry 2010 (class 0 OID 0)
-- Dependencies: 175
-- Name: EXTENSION plpgsql; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION plpgsql IS 'PL/pgSQL procedural language';


SET search_path = public, pg_catalog;

SET default_with_oids = false;

--
-- TOC entry 172 (class 1259 OID 4764139)
-- Name: category; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE category (
    category_id smallint NOT NULL,
    category_name character varying(15) NOT NULL,
    description text,
    picture bytea
);


--
-- TOC entry 173 (class 1259 OID 4764157)
-- Name: customer; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE customer (
    customer_id bpchar NOT NULL,
    company_name character varying(40) NOT NULL,
    contact_name character varying(30),
    contact_title character varying(30),
    address character varying(60),
    city character varying(15),
    region character varying(15),
    postal_code character varying(10),
    country character varying(15),
    phone character varying(24),
    fax character varying(24)
);


--
-- TOC entry 174 (class 1259 OID 4764181)
-- Name: product; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE product (
    product_id smallint NOT NULL,
    product_name character varying(40) NOT NULL,
    supplier_id smallint,
    category_id smallint,
    quantity_per_unit character varying(20),
    unit_price real,
    units_in_stock smallint,
    units_on_order smallint,
    reorder_level smallint,
    discontinued integer NOT NULL
);


--
-- TOC entry 1890 (class 2606 OID 4764212)
-- Name: pk_categories; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY category
    ADD CONSTRAINT pk_categories PRIMARY KEY (category_id);


--
-- TOC entry 1892 (class 2606 OID 4764218)
-- Name: pk_customers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY customer
    ADD CONSTRAINT pk_customers PRIMARY KEY (customer_id);


--
-- TOC entry 1894 (class 2606 OID 4764228)
-- Name: pk_products; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY product
    ADD CONSTRAINT pk_products PRIMARY KEY (product_id);


-- Completed on 2015-07-13 22:07:52

--
-- PostgreSQL database dump complete
--


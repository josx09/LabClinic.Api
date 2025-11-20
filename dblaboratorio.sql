-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1
-- Tiempo de generación: 18-08-2025 a las 19:32:23
-- Versión del servidor: 10.4.32-MariaDB
-- Versión de PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de datos: `dblaboratorio`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `appointment_list`
--

CREATE TABLE `appointment_list` (
  `id` int(11) NOT NULL,
  `code` varchar(100) NOT NULL,
  `schedule` datetime NOT NULL,
  `client_id` int(11) NOT NULL,
  `prescription_path` text DEFAULT NULL,
  `status` tinyint(1) NOT NULL DEFAULT 0,
  `date_created` datetime NOT NULL DEFAULT current_timestamp(),
  `date_updated` datetime DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `appointment_list`
--

INSERT INTO `appointment_list` (`id`, `code`, `schedule`, `client_id`, `prescription_path`, `status`, `date_created`, `date_updated`) VALUES
(1, 'AP-0001', '2025-08-10 23:51:23', 1, NULL, 0, '2025-08-09 23:51:23', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `appointment_test_list`
--

CREATE TABLE `appointment_test_list` (
  `id` int(11) NOT NULL,
  `appointment_id` int(11) NOT NULL,
  `test_id` int(11) NOT NULL,
  `price` decimal(12,2) NOT NULL DEFAULT 0.00,
  `status` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `appointment_test_list`
--

INSERT INTO `appointment_test_list` (`id`, `appointment_id`, `test_id`, `price`, `status`) VALUES
(1, 1, 1, 85.00, 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `bono`
--

CREATE TABLE `bono` (
  `id_bono` int(11) NOT NULL,
  `id_empleado` int(11) NOT NULL,
  `nombre_bono` varchar(120) NOT NULL,
  `monto_bono` decimal(12,2) NOT NULL DEFAULT 0.00,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cargo`
--

CREATE TABLE `cargo` (
  `id_cargo` int(11) NOT NULL,
  `nombre_cargo` varchar(120) NOT NULL,
  `funcion` varchar(255) DEFAULT NULL,
  `requisito` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `cargo`
--

INSERT INTO `cargo` (`id_cargo`, `nombre_cargo`, `funcion`, `requisito`) VALUES
(1, 'Recepcionista', 'Atención al cliente', NULL),
(2, 'Técnico Lab', 'Procesa muestras', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `categoriainsumo`
--

CREATE TABLE `categoriainsumo` (
  `id_categoria_insumo` int(11) NOT NULL,
  `nombre_categoria` varchar(150) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `categoriainsumo`
--

INSERT INTO `categoriainsumo` (`id_categoria_insumo`, `nombre_categoria`, `descripcion`) VALUES
(1, 'Reactivos', 'Reactivos de laboratorio'),
(2, 'Material', 'Material descartable');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cita`
--

CREATE TABLE `cita` (
  `id_cita` int(11) NOT NULL,
  `id_paciente` int(11) NOT NULL,
  `id_medico` int(11) DEFAULT NULL,
  `id_fecha` int(11) NOT NULL,
  `estado_cita` tinyint(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `client_list`
--

CREATE TABLE `client_list` (
  `id` int(11) NOT NULL,
  `code` varchar(100) NOT NULL,
  `firstname` varchar(150) NOT NULL,
  `middlename` varchar(150) DEFAULT NULL,
  `lastname` varchar(150) NOT NULL,
  `gender` varchar(20) DEFAULT NULL,
  `contact` varchar(100) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `status` tinyint(1) NOT NULL DEFAULT 1,
  `date_created` datetime NOT NULL DEFAULT current_timestamp(),
  `date_updated` datetime DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `client_list`
--

INSERT INTO `client_list` (`id`, `code`, `firstname`, `middlename`, `lastname`, `gender`, `contact`, `address`, `status`, `date_created`, `date_updated`) VALUES
(1, 'CL-0001', 'Juan', NULL, 'Pérez', 'M', '5555-1234', 'Zona 1, Guatemala', 1, '2025-08-09 23:51:23', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `cotizacion`
--

CREATE TABLE `cotizacion` (
  `id_cotizacion` int(11) NOT NULL,
  `id_examen` int(11) NOT NULL,
  `precio` decimal(12,2) NOT NULL DEFAULT 0.00,
  `fecha_creacion` datetime NOT NULL DEFAULT current_timestamp(),
  `id_pago` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `departamento`
--

CREATE TABLE `departamento` (
  `idDepartamento` int(11) NOT NULL,
  `nombre` varchar(120) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `departamento`
--

INSERT INTO `departamento` (`idDepartamento`, `nombre`) VALUES
(1, 'Guatemala'),
(2, 'Sacatepéquez');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `descuento`
--

CREATE TABLE `descuento` (
  `id_descuento` int(11) NOT NULL,
  `id_empleado` int(11) NOT NULL,
  `monto` decimal(12,2) NOT NULL DEFAULT 0.00,
  `descripcion` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `direccion`
--

CREATE TABLE `direccion` (
  `id_direccion` int(11) NOT NULL,
  `calle` varchar(150) DEFAULT NULL,
  `numero` varchar(20) DEFAULT NULL,
  `zona` varchar(20) DEFAULT NULL,
  `referencia` varchar(255) DEFAULT NULL,
  `idMunicipio` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `empleado`
--

CREATE TABLE `empleado` (
  `id_empleado` int(11) NOT NULL,
  `id_persona` int(11) NOT NULL,
  `DPI` varchar(30) DEFAULT NULL,
  `idMunicipio` int(11) DEFAULT NULL,
  `formacion_academica` varchar(255) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `empleado`
--

INSERT INTO `empleado` (`id_empleado`, `id_persona`, `DPI`, `idMunicipio`, `formacion_academica`, `estado`) VALUES
(1, 1, '1234567890101', 1, 'Técnico de laboratorio', 1);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `examen`
--

CREATE TABLE `examen` (
  `id_examen` int(11) NOT NULL,
  `id_persona` int(11) NOT NULL,
  `id_tipo_examen` int(11) NOT NULL,
  `id_insumo` int(11) DEFAULT NULL,
  `resultado` text DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `facturapago`
--

CREATE TABLE `facturapago` (
  `id_factura` int(11) NOT NULL,
  `id_pago` int(11) NOT NULL,
  `fecha_factura` datetime DEFAULT NULL,
  `monto_total` decimal(12,2) NOT NULL DEFAULT 0.00,
  `NIT` varchar(50) DEFAULT NULL,
  `Detalle` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `fechacita`
--

CREATE TABLE `fechacita` (
  `id_fecha` int(11) NOT NULL,
  `fecha` date NOT NULL,
  `Estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `ingresoinsumo`
--

CREATE TABLE `ingresoinsumo` (
  `id_ingreso` int(11) NOT NULL,
  `id_insumo` int(11) NOT NULL,
  `cantidad` decimal(12,2) NOT NULL,
  `fecha_ingreso` datetime NOT NULL DEFAULT current_timestamp(),
  `fecha_expira` date DEFAULT NULL,
  `descripcion` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `insumolaboratorio`
--

CREATE TABLE `insumolaboratorio` (
  `id_insumo` int(11) NOT NULL,
  `id_categoria_insumo` int(11) NOT NULL,
  `id_proveedor` int(11) DEFAULT NULL,
  `nombre_insumo` varchar(200) NOT NULL,
  `stock` decimal(12,2) NOT NULL DEFAULT 0.00,
  `stock_minimo` decimal(12,2) NOT NULL DEFAULT 0.00,
  `unidad_medida` varchar(30) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1,
  `almacenado` varchar(100) DEFAULT NULL,
  `precio` decimal(12,2) NOT NULL DEFAULT 0.00,
  `descripcion` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `municipio`
--

CREATE TABLE `municipio` (
  `idMunicipio` int(11) NOT NULL,
  `nombre` varchar(120) NOT NULL,
  `codigo_postal` varchar(10) DEFAULT NULL,
  `activo` tinyint(1) NOT NULL DEFAULT 1,
  `idDepartamento` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `municipio`
--

INSERT INTO `municipio` (`idMunicipio`, `nombre`, `codigo_postal`, `activo`, `idDepartamento`) VALUES
(1, 'Guatemala', NULL, 1, 1),
(2, 'Mixco', NULL, 1, 1),
(3, 'Antigua Guatemala', NULL, 1, 2);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `pago`
--

CREATE TABLE `pago` (
  `id_pago` int(11) NOT NULL,
  `id_usuario` int(11) NOT NULL,
  `monto_pagado` decimal(12,2) NOT NULL DEFAULT 0.00,
  `concepto` varchar(255) DEFAULT NULL,
  `id_tipo_pago` int(11) NOT NULL,
  `fecha_generado` datetime DEFAULT NULL,
  `fecha_pago` datetime DEFAULT NULL,
  `nota` varchar(255) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `persona`
--

CREATE TABLE `persona` (
  `id_persona` int(11) NOT NULL,
  `nombre` varchar(120) NOT NULL,
  `apellido` varchar(120) NOT NULL,
  `sexo` varchar(10) DEFAULT NULL,
  `telefono` varchar(50) DEFAULT NULL,
  `correo` varchar(150) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1,
  `id_direccion` int(11) DEFAULT NULL,
  `idMunicipio` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `persona`
--

INSERT INTO `persona` (`id_persona`, `nombre`, `apellido`, `sexo`, `telefono`, `correo`, `estado`, `id_direccion`, `idMunicipio`) VALUES
(1, 'María jose', 'Gómez', 'F', '5555-6789', 'maria@example.com', 1, NULL, 1),
(2, 'Jose Ivan', 'Gongora', 'Masculino', '4156-9874', 'jose@ejemplo.com', 1, NULL, 2),
(3, '', '', NULL, NULL, NULL, 1, NULL, NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `planilla`
--

CREATE TABLE `planilla` (
  `id_planilla` int(11) NOT NULL,
  `id_empleado` int(11) NOT NULL,
  `puesto` varchar(120) DEFAULT NULL,
  `id_cargo` int(11) NOT NULL,
  `salariobase` decimal(12,2) NOT NULL DEFAULT 0.00,
  `fechainicio` date DEFAULT NULL,
  `fechafin` date DEFAULT NULL,
  `nocuenta` varchar(100) DEFAULT NULL,
  `banco` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `planilla`
--

INSERT INTO `planilla` (`id_planilla`, `id_empleado`, `puesto`, `id_cargo`, `salariobase`, `fechainicio`, `fechafin`, `nocuenta`, `banco`) VALUES
(1, 1, 'Técnico Lab', 2, 3500.00, NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `proveedor`
--

CREATE TABLE `proveedor` (
  `id_proveedor` int(11) NOT NULL,
  `id_persona` int(11) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `estado` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `registrohorariofechas`
--

CREATE TABLE `registrohorariofechas` (
  `id_registro` int(11) NOT NULL,
  `id_fecha` int(11) NOT NULL,
  `hora_inicio` time NOT NULL,
  `hora_fin` time NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `rol`
--

CREATE TABLE `rol` (
  `id_rol` int(11) NOT NULL,
  `nombre_rol` varchar(80) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `test_list`
--

CREATE TABLE `test_list` (
  `id` int(11) NOT NULL,
  `code` varchar(50) NOT NULL,
  `name` varchar(200) NOT NULL,
  `description` text DEFAULT NULL,
  `price` decimal(12,2) NOT NULL DEFAULT 0.00,
  `status` tinyint(1) NOT NULL DEFAULT 1,
  `date_created` datetime NOT NULL DEFAULT current_timestamp(),
  `date_updated` datetime DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `test_list`
--

INSERT INTO `test_list` (`id`, `code`, `name`, `description`, `price`, `status`, `date_created`, `date_updated`) VALUES
(1, 'T-HEMO', 'Hemograma', 'Prueba sanguínea', 85.00, 1, '2025-08-09 23:51:23', NULL),
(2, 'T-GLU', 'Glucosa', 'Glucosa en sangre', 30.00, 1, '2025-08-09 23:51:23', NULL);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipoexamen`
--

CREATE TABLE `tipoexamen` (
  `id_tipo_examen` int(11) NOT NULL,
  `nombre` varchar(200) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `precio` decimal(12,2) NOT NULL DEFAULT 0.00
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `tipoexamen`
--

INSERT INTO `tipoexamen` (`id_tipo_examen`, `nombre`, `descripcion`, `precio`) VALUES
(1, 'Hemograma completo', 'Conteo sanguíneo', 85.00),
(2, 'Glucosa', 'Glucosa en sangre', 30.00);

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `tipopago`
--

CREATE TABLE `tipopago` (
  `id_tipo_pago` int(11) NOT NULL,
  `nombre_tipo_pago` varchar(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `tipopago`
--

INSERT INTO `tipopago` (`id_tipo_pago`, `nombre_tipo_pago`) VALUES
(1, 'Efectivo'),
(2, 'Tarjeta'),
(3, 'Transferencia');

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `users`
--

CREATE TABLE `users` (
  `id` int(11) NOT NULL,
  `firstname` varchar(250) NOT NULL,
  `middlename` text DEFAULT NULL,
  `lastname` varchar(250) NOT NULL,
  `username` varchar(150) NOT NULL,
  `password` text NOT NULL,
  `avatar` text DEFAULT NULL,
  `last_login` datetime DEFAULT NULL,
  `type` tinyint(1) NOT NULL DEFAULT 1,
  `status` tinyint(1) NOT NULL DEFAULT 1,
  `date_added` datetime NOT NULL DEFAULT current_timestamp(),
  `date_updated` datetime DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `users`
--

INSERT INTO `users` (`id`, `firstname`, `middlename`, `lastname`, `username`, `password`, `avatar`, `last_login`, `type`, `status`, `date_added`, `date_updated`) VALUES
(1, 'Admin', NULL, 'Soul', 'admin', '$2a$11$1Lh120.DX5SaWHTw94xJ1.A1iDFstnSS2UvRBO7GVH6DdvuADYr.G', NULL, NULL, 1, 1, '2025-08-09 23:51:23', '2025-08-14 11:40:49'),
(4, 'Jose', NULL, 'Gongora', 'Josx09', '$2a$11$ieQgAF58xuFJ8KkYfFv8X.MTOoNB/Fu7VI67bfMhmxviRjo6Qbc3S', NULL, NULL, 1, 1, '2025-08-14 11:39:44', NULL),
(5, 'Ivan', NULL, 'Guzman', 'Ivanix', '$2a$11$vMdXwa.PdvMPLGyEN0yxrOCgkL4z/MHlawQZG7nRijWiwrE4IAQTK', NULL, NULL, 0, 1, '2025-08-14 12:15:13', NULL);

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `appointment_list`
--
ALTER TABLE `appointment_list`
  ADD PRIMARY KEY (`id`),
  ADD KEY `fk_appt_client` (`client_id`);

--
-- Indices de la tabla `appointment_test_list`
--
ALTER TABLE `appointment_test_list`
  ADD PRIMARY KEY (`id`),
  ADD KEY `fk_appttest_appt` (`appointment_id`),
  ADD KEY `fk_appttest_test` (`test_id`);

--
-- Indices de la tabla `bono`
--
ALTER TABLE `bono`
  ADD PRIMARY KEY (`id_bono`),
  ADD KEY `fk_bono_emp` (`id_empleado`);

--
-- Indices de la tabla `cargo`
--
ALTER TABLE `cargo`
  ADD PRIMARY KEY (`id_cargo`);

--
-- Indices de la tabla `categoriainsumo`
--
ALTER TABLE `categoriainsumo`
  ADD PRIMARY KEY (`id_categoria_insumo`);

--
-- Indices de la tabla `cita`
--
ALTER TABLE `cita`
  ADD PRIMARY KEY (`id_cita`),
  ADD KEY `fk_cita_paciente` (`id_paciente`),
  ADD KEY `fk_cita_medico` (`id_medico`),
  ADD KEY `fk_cita_fecha` (`id_fecha`);

--
-- Indices de la tabla `client_list`
--
ALTER TABLE `client_list`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_client_code` (`code`);

--
-- Indices de la tabla `cotizacion`
--
ALTER TABLE `cotizacion`
  ADD PRIMARY KEY (`id_cotizacion`),
  ADD KEY `fk_cot_ex` (`id_examen`),
  ADD KEY `fk_cot_pago` (`id_pago`);

--
-- Indices de la tabla `departamento`
--
ALTER TABLE `departamento`
  ADD PRIMARY KEY (`idDepartamento`);

--
-- Indices de la tabla `descuento`
--
ALTER TABLE `descuento`
  ADD PRIMARY KEY (`id_descuento`),
  ADD KEY `fk_desc_emp` (`id_empleado`);

--
-- Indices de la tabla `direccion`
--
ALTER TABLE `direccion`
  ADD PRIMARY KEY (`id_direccion`),
  ADD KEY `fk_direccion_municipio` (`idMunicipio`);

--
-- Indices de la tabla `empleado`
--
ALTER TABLE `empleado`
  ADD PRIMARY KEY (`id_empleado`),
  ADD KEY `fk_emp_persona` (`id_persona`),
  ADD KEY `fk_emp_mun` (`idMunicipio`);

--
-- Indices de la tabla `examen`
--
ALTER TABLE `examen`
  ADD PRIMARY KEY (`id_examen`),
  ADD KEY `fk_ex_persona` (`id_persona`),
  ADD KEY `fk_ex_tipo` (`id_tipo_examen`),
  ADD KEY `fk_ex_insumo` (`id_insumo`);

--
-- Indices de la tabla `facturapago`
--
ALTER TABLE `facturapago`
  ADD PRIMARY KEY (`id_factura`),
  ADD KEY `fk_fact_pago` (`id_pago`);

--
-- Indices de la tabla `fechacita`
--
ALTER TABLE `fechacita`
  ADD PRIMARY KEY (`id_fecha`);

--
-- Indices de la tabla `ingresoinsumo`
--
ALTER TABLE `ingresoinsumo`
  ADD PRIMARY KEY (`id_ingreso`),
  ADD KEY `fk_ing_ins` (`id_insumo`);

--
-- Indices de la tabla `insumolaboratorio`
--
ALTER TABLE `insumolaboratorio`
  ADD PRIMARY KEY (`id_insumo`),
  ADD KEY `fk_ins_cat` (`id_categoria_insumo`),
  ADD KEY `fk_ins_prov` (`id_proveedor`);

--
-- Indices de la tabla `municipio`
--
ALTER TABLE `municipio`
  ADD PRIMARY KEY (`idMunicipio`),
  ADD KEY `fk_mun_dept` (`idDepartamento`);

--
-- Indices de la tabla `pago`
--
ALTER TABLE `pago`
  ADD PRIMARY KEY (`id_pago`),
  ADD KEY `fk_pago_user` (`id_usuario`),
  ADD KEY `fk_pago_tipo` (`id_tipo_pago`);

--
-- Indices de la tabla `persona`
--
ALTER TABLE `persona`
  ADD PRIMARY KEY (`id_persona`),
  ADD KEY `fk_persona_mun` (`idMunicipio`),
  ADD KEY `fk_persona_direccion` (`id_direccion`);

--
-- Indices de la tabla `planilla`
--
ALTER TABLE `planilla`
  ADD PRIMARY KEY (`id_planilla`),
  ADD KEY `fk_plan_emp` (`id_empleado`),
  ADD KEY `fk_plan_cargo` (`id_cargo`);

--
-- Indices de la tabla `proveedor`
--
ALTER TABLE `proveedor`
  ADD PRIMARY KEY (`id_proveedor`),
  ADD KEY `fk_prov_persona` (`id_persona`);

--
-- Indices de la tabla `registrohorariofechas`
--
ALTER TABLE `registrohorariofechas`
  ADD PRIMARY KEY (`id_registro`),
  ADD KEY `fk_reg_fecha` (`id_fecha`);

--
-- Indices de la tabla `rol`
--
ALTER TABLE `rol`
  ADD PRIMARY KEY (`id_rol`),
  ADD UNIQUE KEY `ux_rol_nombre` (`nombre_rol`);

--
-- Indices de la tabla `test_list`
--
ALTER TABLE `test_list`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `ux_test_code` (`code`);

--
-- Indices de la tabla `tipoexamen`
--
ALTER TABLE `tipoexamen`
  ADD PRIMARY KEY (`id_tipo_examen`);

--
-- Indices de la tabla `tipopago`
--
ALTER TABLE `tipopago`
  ADD PRIMARY KEY (`id_tipo_pago`),
  ADD UNIQUE KEY `ux_tipopago_nombre` (`nombre_tipo_pago`);

--
-- Indices de la tabla `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `username` (`username`);

--
-- AUTO_INCREMENT de las tablas volcadas
--

--
-- AUTO_INCREMENT de la tabla `appointment_list`
--
ALTER TABLE `appointment_list`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `appointment_test_list`
--
ALTER TABLE `appointment_test_list`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `bono`
--
ALTER TABLE `bono`
  MODIFY `id_bono` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `cargo`
--
ALTER TABLE `cargo`
  MODIFY `id_cargo` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `categoriainsumo`
--
ALTER TABLE `categoriainsumo`
  MODIFY `id_categoria_insumo` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `cita`
--
ALTER TABLE `cita`
  MODIFY `id_cita` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `client_list`
--
ALTER TABLE `client_list`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `cotizacion`
--
ALTER TABLE `cotizacion`
  MODIFY `id_cotizacion` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `departamento`
--
ALTER TABLE `departamento`
  MODIFY `idDepartamento` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `descuento`
--
ALTER TABLE `descuento`
  MODIFY `id_descuento` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `direccion`
--
ALTER TABLE `direccion`
  MODIFY `id_direccion` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `empleado`
--
ALTER TABLE `empleado`
  MODIFY `id_empleado` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `examen`
--
ALTER TABLE `examen`
  MODIFY `id_examen` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `facturapago`
--
ALTER TABLE `facturapago`
  MODIFY `id_factura` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `fechacita`
--
ALTER TABLE `fechacita`
  MODIFY `id_fecha` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `ingresoinsumo`
--
ALTER TABLE `ingresoinsumo`
  MODIFY `id_ingreso` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `insumolaboratorio`
--
ALTER TABLE `insumolaboratorio`
  MODIFY `id_insumo` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `municipio`
--
ALTER TABLE `municipio`
  MODIFY `idMunicipio` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT de la tabla `pago`
--
ALTER TABLE `pago`
  MODIFY `id_pago` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `persona`
--
ALTER TABLE `persona`
  MODIFY `id_persona` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT de la tabla `planilla`
--
ALTER TABLE `planilla`
  MODIFY `id_planilla` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT de la tabla `proveedor`
--
ALTER TABLE `proveedor`
  MODIFY `id_proveedor` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `registrohorariofechas`
--
ALTER TABLE `registrohorariofechas`
  MODIFY `id_registro` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `rol`
--
ALTER TABLE `rol`
  MODIFY `id_rol` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT de la tabla `test_list`
--
ALTER TABLE `test_list`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `tipoexamen`
--
ALTER TABLE `tipoexamen`
  MODIFY `id_tipo_examen` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT de la tabla `tipopago`
--
ALTER TABLE `tipopago`
  MODIFY `id_tipo_pago` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT de la tabla `users`
--
ALTER TABLE `users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- Restricciones para tablas volcadas
--

--
-- Filtros para la tabla `appointment_list`
--
ALTER TABLE `appointment_list`
  ADD CONSTRAINT `fk_appt_client` FOREIGN KEY (`client_id`) REFERENCES `client_list` (`id`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `appointment_test_list`
--
ALTER TABLE `appointment_test_list`
  ADD CONSTRAINT `fk_appttest_appt` FOREIGN KEY (`appointment_id`) REFERENCES `appointment_list` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_appttest_test` FOREIGN KEY (`test_id`) REFERENCES `test_list` (`id`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `bono`
--
ALTER TABLE `bono`
  ADD CONSTRAINT `fk_bono_emp` FOREIGN KEY (`id_empleado`) REFERENCES `empleado` (`id_empleado`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Filtros para la tabla `cita`
--
ALTER TABLE `cita`
  ADD CONSTRAINT `fk_cita_fecha` FOREIGN KEY (`id_fecha`) REFERENCES `fechacita` (`id_fecha`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_cita_medico` FOREIGN KEY (`id_medico`) REFERENCES `persona` (`id_persona`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_cita_paciente` FOREIGN KEY (`id_paciente`) REFERENCES `persona` (`id_persona`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `cotizacion`
--
ALTER TABLE `cotizacion`
  ADD CONSTRAINT `fk_cot_ex` FOREIGN KEY (`id_examen`) REFERENCES `examen` (`id_examen`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_cot_pago` FOREIGN KEY (`id_pago`) REFERENCES `pago` (`id_pago`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Filtros para la tabla `descuento`
--
ALTER TABLE `descuento`
  ADD CONSTRAINT `fk_desc_emp` FOREIGN KEY (`id_empleado`) REFERENCES `empleado` (`id_empleado`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Filtros para la tabla `direccion`
--
ALTER TABLE `direccion`
  ADD CONSTRAINT `fk_direccion_municipio` FOREIGN KEY (`idMunicipio`) REFERENCES `municipio` (`idMunicipio`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `empleado`
--
ALTER TABLE `empleado`
  ADD CONSTRAINT `fk_emp_mun` FOREIGN KEY (`idMunicipio`) REFERENCES `municipio` (`idMunicipio`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_emp_persona` FOREIGN KEY (`id_persona`) REFERENCES `persona` (`id_persona`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `examen`
--
ALTER TABLE `examen`
  ADD CONSTRAINT `fk_ex_insumo` FOREIGN KEY (`id_insumo`) REFERENCES `insumolaboratorio` (`id_insumo`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_ex_persona` FOREIGN KEY (`id_persona`) REFERENCES `persona` (`id_persona`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_ex_tipo` FOREIGN KEY (`id_tipo_examen`) REFERENCES `tipoexamen` (`id_tipo_examen`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `facturapago`
--
ALTER TABLE `facturapago`
  ADD CONSTRAINT `fk_fact_pago` FOREIGN KEY (`id_pago`) REFERENCES `pago` (`id_pago`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Filtros para la tabla `ingresoinsumo`
--
ALTER TABLE `ingresoinsumo`
  ADD CONSTRAINT `fk_ing_ins` FOREIGN KEY (`id_insumo`) REFERENCES `insumolaboratorio` (`id_insumo`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `insumolaboratorio`
--
ALTER TABLE `insumolaboratorio`
  ADD CONSTRAINT `fk_ins_cat` FOREIGN KEY (`id_categoria_insumo`) REFERENCES `categoriainsumo` (`id_categoria_insumo`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_ins_prov` FOREIGN KEY (`id_proveedor`) REFERENCES `proveedor` (`id_proveedor`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Filtros para la tabla `municipio`
--
ALTER TABLE `municipio`
  ADD CONSTRAINT `fk_mun_dept` FOREIGN KEY (`idDepartamento`) REFERENCES `departamento` (`idDepartamento`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `pago`
--
ALTER TABLE `pago`
  ADD CONSTRAINT `fk_pago_tipo` FOREIGN KEY (`id_tipo_pago`) REFERENCES `tipopago` (`id_tipo_pago`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_pago_user` FOREIGN KEY (`id_usuario`) REFERENCES `users` (`id`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `persona`
--
ALTER TABLE `persona`
  ADD CONSTRAINT `fk_persona_direccion` FOREIGN KEY (`id_direccion`) REFERENCES `direccion` (`id_direccion`) ON DELETE SET NULL ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_persona_mun` FOREIGN KEY (`idMunicipio`) REFERENCES `municipio` (`idMunicipio`) ON DELETE SET NULL ON UPDATE CASCADE;

--
-- Filtros para la tabla `planilla`
--
ALTER TABLE `planilla`
  ADD CONSTRAINT `fk_plan_cargo` FOREIGN KEY (`id_cargo`) REFERENCES `cargo` (`id_cargo`) ON UPDATE CASCADE,
  ADD CONSTRAINT `fk_plan_emp` FOREIGN KEY (`id_empleado`) REFERENCES `empleado` (`id_empleado`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `proveedor`
--
ALTER TABLE `proveedor`
  ADD CONSTRAINT `fk_prov_persona` FOREIGN KEY (`id_persona`) REFERENCES `persona` (`id_persona`) ON UPDATE CASCADE;

--
-- Filtros para la tabla `registrohorariofechas`
--
ALTER TABLE `registrohorariofechas`
  ADD CONSTRAINT `fk_reg_fecha` FOREIGN KEY (`id_fecha`) REFERENCES `fechacita` (`id_fecha`) ON DELETE CASCADE ON UPDATE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

-- =========================================================
-- V1: Esquema inicial REDCQ BANK
-- =========================================================

CREATE TABLE tb_personal_banco (
    id_personal     BIGINT       NOT NULL AUTO_INCREMENT,
    nombres         VARCHAR(100) NOT NULL,
    apellidos       VARCHAR(100) NOT NULL,
    email           VARCHAR(150) NOT NULL,
    password_hash   VARCHAR(100) NOT NULL,
    rol             ENUM('ADMIN','SISTEMAS','OPERADOR_ATM','CLIENTE') NOT NULL,
    telefono        VARCHAR(20),
    estado          VARCHAR(20)  NOT NULL,
    created_at      DATETIME(6),
    updated_at      DATETIME(6),
    PRIMARY KEY (id_personal),
    CONSTRAINT UQ_personal_email UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_clientes (
    id_cliente        BIGINT       NOT NULL AUTO_INCREMENT,
    nombres           VARCHAR(100) NOT NULL,
    apellidos         VARCHAR(100) NOT NULL,
    tipo_documento    VARCHAR(20)  NOT NULL,
    numero_documento  VARCHAR(20)  NOT NULL,
    email             VARCHAR(150),
    telefono          VARCHAR(20),
    estado            VARCHAR(20)  NOT NULL,
    created_at        DATETIME(6),
    updated_at        DATETIME(6),
    PRIMARY KEY (id_cliente),
    CONSTRAINT UQ_cliente_documento UNIQUE (tipo_documento, numero_documento),
    CONSTRAINT UQ_cliente_email     UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_parametros (
    id_parametro  INT          NOT NULL AUTO_INCREMENT,
    clave         VARCHAR(60)  NOT NULL,
    valor         VARCHAR(200) NOT NULL,
    tipo_dato     ENUM('INT','DECIMAL','BOOLEAN','STRING') NOT NULL,
    descripcion   VARCHAR(300),
    updated_at    DATETIME(6),
    PRIMARY KEY (id_parametro),
    CONSTRAINT UQ_parametro_clave UNIQUE (clave)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_parametros_historico (
    id_historico       BIGINT       NOT NULL AUTO_INCREMENT,
    id_parametro       INT          NOT NULL,
    clave              VARCHAR(60)  NOT NULL,
    valor_anterior     VARCHAR(200),
    valor_nuevo        VARCHAR(200) NOT NULL,
    id_usuario_cambio  BIGINT,
    motivo             VARCHAR(300),
    fecha_cambio       DATETIME(6)  NOT NULL,
    PRIMARY KEY (id_historico),
    CONSTRAINT FK_param_hist_parametro FOREIGN KEY (id_parametro) REFERENCES tb_parametros(id_parametro),
    CONSTRAINT FK_param_hist_usuario   FOREIGN KEY (id_usuario_cambio) REFERENCES tb_personal_banco(id_personal),
    INDEX IX_param_hist_parametro (id_parametro),
    INDEX IX_param_hist_fecha (fecha_cambio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_tipo_cambio (
    id_tipo_cambio  INT            NOT NULL AUTO_INCREMENT,
    fecha           DATE           NOT NULL,
    tasa_compra     DECIMAL(12,6)  NOT NULL,
    tasa_venta      DECIMAL(12,6)  NOT NULL,
    created_at      DATETIME(6),
    updated_at      DATETIME(6),
    PRIMARY KEY (id_tipo_cambio),
    CONSTRAINT UQ_tipo_cambio_fecha UNIQUE (fecha)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_tipos_cuenta_config (
    id_config                  INT           NOT NULL AUTO_INCREMENT,
    tipo_cuenta                ENUM('AHORROS','CORRIENTE') NOT NULL,
    subtipo_cuenta             ENUM('NORMAL','FREE','SUELDO','NA') NOT NULL,
    comision_mantenimiento     DECIMAL(15,2) NOT NULL,
    tasa_interes_anual         DECIMAL(5,4)  NOT NULL,
    max_operaciones_gratuitas  INT           NOT NULL,
    costo_exceso_operacion     DECIMAL(15,2) NOT NULL,
    aplica_itf                 BOOLEAN       NOT NULL,
    created_at                 DATETIME(6),
    PRIMARY KEY (id_config),
    CONSTRAINT UQ_tipo_subtipo_config UNIQUE (tipo_cuenta, subtipo_cuenta)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_tarjetas (
    id_tarjeta        BIGINT       NOT NULL AUTO_INCREMENT,
    id_cliente        BIGINT       NOT NULL,
    numero_tarjeta    VARCHAR(16)  NOT NULL,
    pin_hash          VARCHAR(200) NOT NULL,
    pin_salt          VARCHAR(100) NOT NULL,
    fecha_expiracion  DATE         NOT NULL,
    estado            ENUM('ACTIVA','BLOQUEADA','VENCIDA') NOT NULL,
    intentos_fallidos INT          NOT NULL,
    debe_cambiar_pin  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at        DATETIME(6),
    PRIMARY KEY (id_tarjeta),
    CONSTRAINT UQ_tarjeta_numero FOREIGN KEY (id_cliente) REFERENCES tb_clientes(id_cliente),
    CONSTRAINT UQ_tarjeta_numero_unico UNIQUE (numero_tarjeta),
    INDEX IX_tarjeta_cliente (id_cliente)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_cuentas (
    id_cuenta                  BIGINT        NOT NULL AUTO_INCREMENT,
    id_tarjeta                 BIGINT        NOT NULL,
    numero_cuenta              VARCHAR(14)   NOT NULL,
    moneda                     ENUM('PEN','USD') NOT NULL,
    tipo_cuenta                ENUM('AHORROS','CORRIENTE') NOT NULL,
    subtipo_cuenta             ENUM('NORMAL','FREE','SUELDO','NA') NOT NULL,
    saldo                      DECIMAL(15,2) NOT NULL CHECK (saldo >= 0),
    contador_operaciones_mes   INT           NOT NULL CHECK (contador_operaciones_mes >= 0),
    estado                     ENUM('ACTIVA','INACTIVA','CERRADA') NOT NULL,
    version                    INT           NOT NULL,
    created_at                 DATETIME(6),
    updated_at                 DATETIME(6),
    PRIMARY KEY (id_cuenta),
    CONSTRAINT UQ_cuenta_numero UNIQUE (numero_cuenta),
    CONSTRAINT FK_cuenta_tarjeta FOREIGN KEY (id_tarjeta) REFERENCES tb_tarjetas(id_tarjeta),
    INDEX IX_cuenta_tarjeta (id_tarjeta)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_atms (
    id_atm      BIGINT       NOT NULL AUTO_INCREMENT,
    codigo      VARCHAR(20)  NOT NULL,
    ubicacion   VARCHAR(200),
    estado      ENUM('EN_SERVICIO','FUERA_SERVICIO','MANTENIMIENTO','CARGA_DINERO') NOT NULL,
    created_at  DATETIME(6),
    PRIMARY KEY (id_atm),
    CONSTRAINT UQ_atm_codigo UNIQUE (codigo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_atm_cash (
    id_atm_cash    BIGINT       NOT NULL AUTO_INCREMENT,
    id_atm         BIGINT       NOT NULL,
    moneda         ENUM('PEN','USD') NOT NULL,
    denominacion   INT          NOT NULL,
    cantidad       INT          NOT NULL CHECK (cantidad >= 0),
    PRIMARY KEY (id_atm_cash),
    CONSTRAINT UQ_atm_denominacion UNIQUE (id_atm, moneda, denominacion),
    CONSTRAINT FK_atm_cash_atm FOREIGN KEY (id_atm) REFERENCES tb_atms(id_atm)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_transacciones (
    id_transaccion                BIGINT        NOT NULL AUTO_INCREMENT,
    id_cuenta                     BIGINT        NOT NULL,
    id_atm                        BIGINT,
    tipo_operacion                ENUM('RETIRO','DEPOSITO','TRANSFERENCIA_ENVIADA','TRANSFERENCIA_RECIBIDA',
                                       'CAMBIO_PIN','COBRO_MANTENIMIENTO','ABONO_INTERES','COBRO_DEUDA') NOT NULL,
    monto                         DECIMAL(15,2) NOT NULL,
    monto_itf                     DECIMAL(15,2) NOT NULL,
    comision_aplicada             DECIMAL(15,2) NOT NULL,
    saldo_anterior                DECIMAL(15,2) NOT NULL,
    saldo_posterior               DECIMAL(15,2) NOT NULL,
    numero_operacion              VARCHAR(30)   NOT NULL,
    itf_porcentaje_aplicado       DECIMAL(5,4),
    comision_porcentaje_aplicado  DECIMAL(5,4),
    tea_porcentaje_aplicado       DECIMAL(5,4),
    tipo_cambio_aplicado          DECIMAL(12,6),
    fecha                         DATETIME(6)   NOT NULL,
    estado                        VARCHAR(20)   NOT NULL,
    PRIMARY KEY (id_transaccion),
    CONSTRAINT UQ_transaccion_numero UNIQUE (numero_operacion),
    CONSTRAINT FK_transaccion_cuenta FOREIGN KEY (id_cuenta) REFERENCES tb_cuentas(id_cuenta),
    CONSTRAINT FK_transaccion_atm    FOREIGN KEY (id_atm)    REFERENCES tb_atms(id_atm),
    INDEX IX_transaccion_cuenta (id_cuenta),
    INDEX IX_transaccion_fecha (fecha),
    INDEX IX_transaccion_tipo (tipo_operacion)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_transferencias (
    id_transferencia         BIGINT        NOT NULL AUTO_INCREMENT,
    id_transaccion_origen    BIGINT        NOT NULL,
    id_transaccion_destino   BIGINT        NOT NULL,
    cuenta_origen            VARCHAR(14)   NOT NULL,
    cuenta_destino           VARCHAR(14)   NOT NULL,
    monto_origen             DECIMAL(15,2) NOT NULL,
    monto_destino            DECIMAL(15,2) NOT NULL,
    tipo_cambio_aplicado     DECIMAL(12,6),
    PRIMARY KEY (id_transferencia),
    CONSTRAINT FK_transf_trans_origen  FOREIGN KEY (id_transaccion_origen)  REFERENCES tb_transacciones(id_transaccion),
    CONSTRAINT FK_transf_trans_destino FOREIGN KEY (id_transaccion_destino) REFERENCES tb_transacciones(id_transaccion),
    INDEX IX_transf_origen (cuenta_origen),
    INDEX IX_transf_destino (cuenta_destino)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_saldos_deudores (
    id_deuda                     BIGINT        NOT NULL AUTO_INCREMENT,
    id_cuenta                    BIGINT        NOT NULL,
    tipo_cargo                   ENUM('MANTENIMIENTO','ITF','COMISION_EXCESO','OTRO') NOT NULL,
    monto                        DECIMAL(15,2) NOT NULL,
    moneda                       ENUM('PEN','USD') NOT NULL,
    estado                       ENUM('PENDIENTE','APLICADO','ANULADO') NOT NULL,
    fecha_generacion             DATETIME(6)   NOT NULL,
    fecha_aplicacion             DATETIME(6),
    id_transaccion_aplicacion    BIGINT,
    descripcion                  VARCHAR(300),
    PRIMARY KEY (id_deuda),
    CONSTRAINT FK_deuda_cuenta FOREIGN KEY (id_cuenta) REFERENCES tb_cuentas(id_cuenta),
    CONSTRAINT FK_deuda_transaccion FOREIGN KEY (id_transaccion_aplicacion) REFERENCES tb_transacciones(id_transaccion),
    INDEX IX_deuda_cuenta_estado (id_cuenta, estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_atm_sesiones (
    id_sesion     BIGINT       NOT NULL AUTO_INCREMENT,
    id_tarjeta    BIGINT       NOT NULL,
    id_atm        BIGINT       NOT NULL,
    fecha_inicio  DATETIME(6)  NOT NULL,
    fecha_fin     DATETIME(6),
    estado        ENUM('ACTIVA','CERRADA','TIMEOUT','CANCELADA') NOT NULL,
    motivo_cierre VARCHAR(50),
    ip_origen     VARCHAR(45),
    PRIMARY KEY (id_sesion),
    CONSTRAINT FK_sesion_tarjeta FOREIGN KEY (id_tarjeta) REFERENCES tb_tarjetas(id_tarjeta),
    CONSTRAINT FK_sesion_atm     FOREIGN KEY (id_atm)     REFERENCES tb_atms(id_atm),
    INDEX IX_sesion_tarjeta (id_tarjeta),
    INDEX IX_sesion_estado  (estado),
    INDEX IX_sesion_inicio  (fecha_inicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_solicitudes_tarjeta (
    id_solicitud        BIGINT       NOT NULL AUTO_INCREMENT,
    id_cliente          BIGINT       NOT NULL,
    id_tarjeta_anterior BIGINT,
    motivo              VARCHAR(20)  NOT NULL,
    estado              ENUM('PENDIENTE','ATENDIDA','RECHAZADA') NOT NULL,
    fecha_solicitud     DATETIME(6)  NOT NULL,
    fecha_atencion      DATETIME(6),
    id_admin_atendio    BIGINT,
    PRIMARY KEY (id_solicitud),
    CONSTRAINT FK_solicitud_cliente FOREIGN KEY (id_cliente) REFERENCES tb_clientes(id_cliente),
    CONSTRAINT FK_solicitud_tarjeta FOREIGN KEY (id_tarjeta_anterior) REFERENCES tb_tarjetas(id_tarjeta),
    CONSTRAINT FK_solicitud_admin   FOREIGN KEY (id_admin_atendio) REFERENCES tb_personal_banco(id_personal),
    INDEX IX_solicitud_cliente (id_cliente),
    INDEX IX_solicitud_estado (estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_contadores_operacion (
    id_contador    INT          NOT NULL AUTO_INCREMENT,
    id_atm         BIGINT       NOT NULL,
    fecha          DATE         NOT NULL,
    ultimo_numero  INT          NOT NULL,
    PRIMARY KEY (id_contador),
    CONSTRAINT UQ_contador_atm_fecha UNIQUE (id_atm, fecha),
    CONSTRAINT FK_contador_atm FOREIGN KEY (id_atm) REFERENCES tb_atms(id_atm)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_atm_cash_movimientos (
    id_movimiento     BIGINT       NOT NULL AUTO_INCREMENT,
    id_atm            BIGINT       NOT NULL,
    id_atm_cash       BIGINT,
    tipo_movimiento   ENUM('REPOSICION','RETIRO_OPERADOR','DISPENSADO','AJUSTE') NOT NULL,
    moneda            ENUM('PEN','USD') NOT NULL,
    denominacion      INT          NOT NULL,
    cantidad          INT          NOT NULL,
    id_operador       BIGINT,
    id_transaccion    BIGINT,
    saldo_anterior    INT          NOT NULL,
    saldo_posterior   INT          NOT NULL,
    fecha             DATETIME(6)  NOT NULL,
    observacion       VARCHAR(300),
    PRIMARY KEY (id_movimiento),
    CONSTRAINT FK_mov_atm       FOREIGN KEY (id_atm)       REFERENCES tb_atms(id_atm),
    CONSTRAINT FK_mov_atm_cash  FOREIGN KEY (id_atm_cash)  REFERENCES tb_atm_cash(id_atm_cash),
    CONSTRAINT FK_mov_operador  FOREIGN KEY (id_operador)  REFERENCES tb_personal_banco(id_personal),
    CONSTRAINT FK_mov_transaccion FOREIGN KEY (id_transaccion) REFERENCES tb_transacciones(id_transaccion),
    INDEX IX_cash_mov_atm (id_atm),
    INDEX IX_cash_mov_fecha (fecha)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tb_auditoria (
    id_auditoria  BIGINT       NOT NULL AUTO_INCREMENT,
    tipo_actor    ENUM('PERSONAL','CLIENTE','SISTEMA') NOT NULL,
    id_actor      BIGINT,
    email_actor   VARCHAR(150),
    operacion     VARCHAR(60)  NOT NULL,
    recurso       VARCHAR(200),
    resultado     VARCHAR(20)  NOT NULL,
    detalle       VARCHAR(500),
    ip            VARCHAR(45),
    user_agent    VARCHAR(300),
    timestamp     DATETIME(6)  NOT NULL,
    PRIMARY KEY (id_auditoria),
    INDEX IX_auditoria_timestamp (timestamp),
    INDEX IX_auditoria_actor (tipo_actor, id_actor),
    INDEX IX_auditoria_operacion (operacion, resultado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
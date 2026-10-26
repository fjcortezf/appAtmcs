-- =========================================================
-- V2: Datos semilla estáticos
-- =========================================================

-- Parámetros
INSERT INTO tb_parametros (clave, valor, tipo_dato, descripcion) VALUES
('ITF_PORCENTAJE',             '0.00005', 'DECIMAL', 'Porcentaje de ITF aplicado a operaciones'),
('MANTENIMIENTO_PORCENTAJE',   '0.0200',  'DECIMAL', 'Porcentaje de mantenimiento mensual'),
('TEA_PORCENTAJE',             '0.0300',  'DECIMAL', 'Tasa efectiva anual de interés'),
('COSTO_EXCESO_OPERACION',     '5.00',    'DECIMAL', 'Costo por operación excedente'),
('LIMITE_RETIRO_DIARIO',       '5000.00', 'DECIMAL', 'Límite diario de retiro por tarjeta'),
('LIMITE_RETIRO_OPERACION',    '1000.00', 'DECIMAL', 'Límite por operación de retiro'),
('MAX_OPERACIONES_GRATUITAS',  '10',      'INT',     'Máximo de operaciones gratuitas al mes'),
('TIMEOUT_SESION_SEGUNDOS',    '120',     'INT',     'Segundos de inactividad antes de cerrar sesión'),
('TIMEOUT_AVISO_SEGUNDOS',     '10',      'INT',     'Segundos previos al cierre para mostrar aviso'),
('HORARIO_ATENCION_INICIO',    '06:00',   'STRING',  'Hora de inicio de atención'),
('HORARIO_ATENCION_FIN',       '22:00',   'STRING',  'Hora de fin de atención'),
('ITF_UMBRAL_PEN',             '1000.00', 'DECIMAL', 'Monto mínimo en PEN para aplicar ITF'),
('ITF_UMBRAL_USD',             '350.00',  'DECIMAL', 'Monto mínimo en USD para aplicar ITF'),
('MAX_OPERACIONES_SIN_ITF',    '2',       'INT',     'Cantidad de operaciones iniciales exentas de ITF'),
('UIT_VALOR',                  '5150.00', 'DECIMAL', 'Valor referencial de la UIT');

-- Tipos de cuenta config
INSERT INTO tb_tipos_cuenta_config
(tipo_cuenta, subtipo_cuenta, comision_mantenimiento, tasa_interes_anual,
 max_operaciones_gratuitas, costo_exceso_operacion, aplica_itf) VALUES
('AHORROS',   'NORMAL', 2.00, 0.0300, 10,   5.00, TRUE),
('AHORROS',   'FREE',   0.00, 0.0000, 9999, 0.00, FALSE),
('AHORROS',   'SUELDO', 0.00, 0.0500, 15,   3.00, TRUE),
('CORRIENTE', 'NA',     5.00, 0.0100, 20,   4.00, TRUE);

-- ATM
INSERT INTO tb_atms (codigo, ubicacion, estado) VALUES
('ATM01', 'Oficina Principal', 'EN_SERVICIO');

-- Denominaciones del ATM (asumiendo que el id_atm = 1)
INSERT INTO tb_atm_cash (id_atm, moneda, denominacion, cantidad) VALUES
(1, 'PEN', 20,  100),
(1, 'PEN', 50,  100),
(1, 'PEN', 100, 50),
(1, 'PEN', 200, 20),
(1, 'USD', 20,  50),
(1, 'USD', 50,  50),
(1, 'USD', 100, 50);
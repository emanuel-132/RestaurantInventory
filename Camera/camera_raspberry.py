import cv2
import requests
import time
from picamera2 import Picamera2
from libcamera import controls

# API endpoint
API_URL = "http://localhost:5141/api/Sensores/lectura"

# Initialize Raspberry Pi camera using Picamera2 with higher resolution for better quality
print("Inicializando Picamera2 con alta calidad...")
picam2 = Picamera2()

# Configure for 1280x720 HD resolution with RGB888 format for crisp text/QR detection
config = picam2.create_preview_configuration(
    main={"size": (1280, 720), "format": "RGB888"}
)
picam2.configure(config)
picam2.start()
# ACTIVAR ENFOQUE AUTOMÁTICO CONTINUO (Específico para la Cámara V3)
picam2.set_controls({"AfMode": controls.AfModeEnum.Continuous})

# Opcional: Forzar un ciclo de enfoque inmediato al arrancar
picam2.autofocus_cycle()

detector = cv2.QRCodeDetector()

# Diccionario para llevar el control inteligente: { "CODIGO_QR": frames_ausente }
qrs_en_vista_actual = set()
contador_ausencia = {}

# Cuántos fotogramas consecutivos debe dejar de verse el QR para considerarlo "retirado"
LIMITE_FOTOGRAMAS_AUSENCIA = 150

print("Scanner inteligente iniciado. Presiona 'q' para salir.")

try:
    while True:
        # Capturar fotograma desde Picamera2 como un arreglo de NumPy
        frame = picam2.capture_array()
        if frame is None:
            print("Error: No se pudo capturar el fotograma.")
            break

        # Convertir de RGB (Picamera2) a BGR (OpenCV) para visualización correcta de colores
        frame_bgr = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)

        # Detectar todos los códigos QR visibles en este fotograma actual
        data, bbox, _ = detector.detectAndDecode(frame_bgr)

        # Creamos un set temporal con lo que la cámara está viendo en este exacto milisegundo
        qrs_detectados_ahora = set()
        if bbox is not None and data:
            qrs_detectados_ahora.add(data)

            # Dibujar cuadro visual del QR en pantalla
            for i in range(len(bbox[0])):
                pt1 = tuple(bbox[0][i].astype(int))
                pt2 = tuple(bbox[0][(i + 1) % len(bbox[0])].astype(int))
                cv2.line(frame_bgr, pt1, pt2, (0, 255, 0), 2)
            cv2.putText(
                frame_bgr, data, tuple(bbox[0][0].astype(int)),
                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 0, 0), 2
            )

        # 1. EVALUAR NUEVOS QRS QUE APARECERÁN FRENTE A LA CÁMARA (INGRESO)
        for qr in qrs_detectados_ahora:
            if qr not in qrs_en_vista_actual:
                print(f"--> ¡PRODUCTO DETECTADO EN ESTANTE!: {qr}")

                # Preparamos el payload para notificar el ingreso al servidor de C#
                payload = {
                    "CodigoQR": qr,
                    "pesoTotalGramos": 500.0  # Aquí luego integrarás tu sensor HX711 real
                }

                try:
                    response = requests.post(API_URL, json=payload, timeout=3)
                    if response.ok:
                        print("Servidor notificado: Ingreso/Lectura exitosa.")
                    else:
                        print(f"Servidor rechazó el ingreso. Código: {response.status_code}")
                except requests.exceptions.RequestException as e:
                    print("Error de conexión con el backend:", e)

                # Reiniciamos su contador de ausencia por si acaso
                contador_ausencia[qr] = 0

            # Mantenemos vivo el código en la vista actual
            qrs_en_vista_actual.add(qr)

        # 2. EVALUAR QRS QUE SE FUERON O FUERON RETIRADOS DEL ESTANTE (SALIDA)
        qrs_para_remover = []
        for qr_previo in list(qrs_en_vista_actual):
            if qr_previo not in qrs_detectados_ahora:
                # Incrementamos su contador de ausencia
                contador_ausencia[qr_previo] = contador_ausencia.get(qr_previo, 0) + 1

                # Si pasa el límite de tiempo sin verse, asumimos que se lo llevaron físicamente
                if contador_ausencia[qr_previo] >= LIMITE_FOTOGRAMAS_AUSENCIA:
                    print(f"<-- ¡PRODUCTO RETIRADO/ACABADO!: {qr_previo}")

                    # Preparamos el payload para notificar la retirada al servidor de C#
                    payload_salida = {
                        "CodigoQR": qr_previo,
                        "pesoTotalGramos": 0.0  # O el valor que indique que se retiró
                    }

                    try:
                        response = requests.post(API_URL, json=payload_salida, timeout=3)
                        if response.ok:
                            print("Servidor notificado: Salida/Retiro exitoso.")
                        else:
                            print(f"Servidor rechazó el retiro. Código: {response.status_code}")
                    except requests.exceptions.RequestException as e:
                        print("Error de conexión con el backend al retirar:", e)

                    qrs_para_remover.append(qr_previo)

        # Limpiamos los que ya se confirmaron como ausentes de nuestra memoria activa
        for qr_fuera in qrs_para_remover:
            qrs_en_vista_actual.remove(qr_fuera)
            del contador_ausencia[qr_fuera]

        cv2.imshow("Smart Inventory Scanner", frame_bgr)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

finally:
    picam2.stop()
    cv2.destroyAllWindows()
    print("Scanner finalizado.")
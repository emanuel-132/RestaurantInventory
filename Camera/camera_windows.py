
import cv2
import requests
import time

# API endpoint
API_URL = "http://localhost:5141/api/Sensores/lectura"

# Initialize webcam
print("Inicializando cámara...")

# 0 = Default webcam
cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)

# Configure resolution to 1280x720
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)

if not cap.isOpened():
    print("Error: No se pudo abrir la cámara.")
    exit()

detector = cv2.QRCodeDetector()

# Track currently visible QR codes
qrs_en_vista_actual = set()
contador_ausencia = {}

# Number of consecutive frames before considering a QR removed
LIMITE_FOTOGRAMAS_AUSENCIA = 150

print("Scanner inteligente iniciado. Presiona 'q' para salir.")

try:
    while True:

        # Capture frame from webcam
        ret, frame = cap.read()

        if not ret:
            print("Error: No se pudo capturar el fotograma.")
            break

        # Detect QR code
        retval, decoded_info, points, _ = detector.detectAndDecodeMulti(frame)

        qrs_detectados_ahora = set()

        if retval and points is not None:
            for qr, qr_points in zip(decoded_info, points):
                # Ignore QR codes that were detected but not decoded
                if not qr:
                    continue

                qrs_detectados_ahora.add(qr)

                # Draw bounding box around each QR
                qr_points = qr_points.astype(int)

                for i in range(4):

                    pt1 = tuple(qr_points[i])
                    pt2 = tuple(qr_points[(i + 1) % 4])

                    cv2.line(
                        frame,
                        pt1,
                        pt2,
                        (0, 255, 0),
                        3
                    )

                # Display QR content
                cv2.putText(
                    frame,
                    qr,
                    tuple(qr_points[0]),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.6,
                    (255, 0, 0),
                    2
                )


        # 1. Detect new QR codes (PRODUCT ENTRY)
        for qr in qrs_detectados_ahora:

            if qr not in qrs_en_vista_actual:

                print(f"--> ¡PRODUCTO DETECTADO EN ESTANTE!: {qr}")

                payload = {
                    "CodigoQR": qr,
                    "pesoTotalGramos": 500.0
                }

                try:
                    response = requests.post(
                        API_URL,
                        json=payload,
                        timeout=3
                    )

                    if response.ok:
                        print("Servidor notificado: Ingreso/Lectura exitosa.")
                    else:
                        print(
                            f"Servidor rechazó el ingreso. "
                            f"Código: {response.status_code}"
                        )

                except requests.exceptions.RequestException as e:
                    print("Error de conexión con el backend:", e)

                contador_ausencia[qr] = 0

            # Keep QR in current view
            qrs_en_vista_actual.add(qr)

            # Reset absence counter when QR appears again
            contador_ausencia[qr] = 0

        # 2. Detect removed QR codes (PRODUCT EXIT)
        qrs_para_remover = []

        for qr_previo in list(qrs_en_vista_actual):

            if qr_previo not in qrs_detectados_ahora:

                contador_ausencia[qr_previo] = (
                    contador_ausencia.get(qr_previo, 0) + 1
                )

                if contador_ausencia[qr_previo] >= LIMITE_FOTOGRAMAS_AUSENCIA:

                    print(f"<-- ¡PRODUCTO RETIRADO/ACABADO!: {qr_previo}")

                    payload_salida = {
                        "CodigoQR": qr_previo,
                        "pesoTotalGramos": 0.0
                    }

                    try:
                        response = requests.post(
                            API_URL,
                            json=payload_salida,
                            timeout=3
                        )

                        if response.ok:
                            print("Servidor notificado: Salida/Retiro exitosa.")
                        else:
                            print(
                                f"Servidor rechazó el retiro. "
                                f"Código: {response.status_code}"
                            )

                    except requests.exceptions.RequestException as e:
                        print("Error de conexión con el backend al retirar:", e)

                    qrs_para_remover.append(qr_previo)

        # Remove QR codes confirmed as absent
        for qr_fuera in qrs_para_remover:

            qrs_en_vista_actual.remove(qr_fuera)
            del contador_ausencia[qr_fuera]

        # Display webcam feed
        cv2.imshow("Smart Inventory Scanner", frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

finally:
    cap.release()
    cv2.destroyAllWindows()

    print("Scanner finalizado.")

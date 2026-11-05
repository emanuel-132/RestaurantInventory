    function seleccionarTodos(seleccionar) {
        document.querySelectorAll('.qr-checkbox').forEach(checkbox => {
            checkbox.checked = seleccionar;
        });
    }

    function imprimirSeleccionados() {

        const seleccionados = document.querySelectorAll('.qr-checkbox:checked');

        if (seleccionados.length === 0) {
            alert('Seleccione al menos una etiqueta QR para imprimir.');
            return;
        }

        document.querySelectorAll('.qr-item').forEach(item => {
            item.classList.remove('selected');
        });

        seleccionados.forEach(checkbox => {
            checkbox.closest('.qr-item').classList.add('selected');
        });

        window.print();
    }

    window.addEventListener('afterprint', () => {
        document.querySelectorAll('.qr-item').forEach(item => {
            item.classList.remove('selected');
        });
    });
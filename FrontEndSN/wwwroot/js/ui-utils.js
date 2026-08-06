window.notifier = {
    // Notificación rápida en la esquina (Toast)
    success: function (message) {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer)
                toast.addEventListener('mouseleave', Swal.resumeTimer)
            }
        });
        Toast.fire({
            icon: 'success',
            title: message,
            theme: 'auto'
        });
    },
    // Mensaje de error
    error: function (message) {
        Swal.fire({
            icon: 'error',
            title: '¡Error!',
            text: message,
            confirmButtonColor: '#0d6efd',
            theme: 'auto'
        });
    },
    // Mensaje para confirmar
    confirm: async function (title, text) {
        const result = await Swal.fire({
            title: title,
            text: text,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#0d6efd',
            cancelButtonColor: '#dc3545',
            confirmButtonText: 'Sí, continuar',
            cancelButtonText: 'Cancelar',
            theme: 'auto'
        });
        return result.isConfirmed;
    }
}; 


window.DescargarArchivos = async (contentStreamReference, nombre) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = nombre;
    anchorElement.click();

    anchorElement.remove();
    URL.revokeObjectURL(url);
};

window.AbrirImpresion = (html) => {
    //  Convertimos el string HTML en un objeto binario (Blob)
    const blob = new Blob([html], { type: 'text/html;charset=utf-8;' });

   
    const url = URL.createObjectURL(blob);


    window.open(url, "_blank", "noopener=yes");

    // Limpieza de memoria en la pestaña
    setTimeout(() => {
        URL.revokeObjectURL(url);
    }, 5000);
};

window.abrirArchivoEnNuevaPestana = async (streamReferencia, mimeType) => {
    // 1. Leemos los bytes que nos manda Blazor
    const arrayBuffer = await streamReferencia.arrayBuffer();

    // 2. Creamos un archivo temporal en la memoria del navegador.
    // PASAR EL MIME TYPE ES VITAL: Si no lo pasas, el navegador no sabrá si es un PDF o una foto y forzará la descarga.
    const blob = new Blob([arrayBuffer], { type: mimeType });

    // 3. Generamos una URL interna temporal para ese archivo
    const url = URL.createObjectURL(blob);

    // 4. Abrimos esa URL en una nueva pestaña
    window.open(url, '_blank');

    // 5. Limpiamos la memoria después de unos segundos para que el navegador no se ponga lento
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};
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
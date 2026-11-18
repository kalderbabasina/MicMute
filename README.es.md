# MicMute

Una utilidad para silenciar el micrófono en Windows desde la bandeja, un indicador en pantalla o un atajo global. Usa WinForms y .NET Framework 4.8, sin paquetes NuGet.

[English README](README.md) · [Русская инструкция](README.ru.md)

[Descargar instalador](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-Setup.exe) · [ZIP portable](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-portable.zip) · [Versiones y SHA-256](https://github.com/kalderbabasina/MicMute/releases)

## Instalación y uso

Necesitas Windows 10 o posterior y .NET Framework 4.8 o posterior. Ejecuta `MicMute-Setup.exe` para instalarlo sin permisos de administrador, o descomprime `MicMute-portable.zip` en una carpeta con permisos de escritura y abre `MicMute2.exe`. Los ejecutables no tienen firma digital; comprueba las sumas SHA-256 publicadas junto a la descarga.

- En el primer inicio manual se abre la configuración. El idioma inicial es inglés; puedes elegir ruso, español o ucraniano coloquial (**Українська (суржик)**) en **General → Language**.
- En **General**, activa o desactiva **Iniciar al entrar en Windows** y pulsa **Aceptar**. MicMute usa el mismo acceso directo que el instalador, inicia sin abrir la configuración ni mostrar un aviso de segunda instancia y no cambia el estado del micrófono. Si Windows ha desactivado MicMute, comprueba **Configuración → Aplicaciones → Inicio**.
- MicMute comprueba las versiones de GitHub una vez al iniciar. Puedes desactivar la comprobación en **General** o hacerla desde la bandeja. La versión instalada solicita permiso para descargar y, tras verificar SHA-256, para ejecutar el instalador. La versión portable abre la página de descarga.
- Selecciona el micrófono predeterminado para comunicaciones de Windows o un dispositivo de captura concreto.
- Pulsa **F13** (atajo predeterminado) o haz clic en el icono de la bandeja o en el indicador para silenciar o reactivar el micrófono. Puedes cambiar el atajo en la configuración.
- Arrastra el indicador para moverlo, o mantén pulsada **Mayús** mientras lo arrastras para cambiar el tamaño (40–256 px). Puedes ocultarlo, bloquearlo y ajustar su opacidad. Un **?** gris indica que no se pudo leer el estado, no que el micrófono esté silenciado.

## Apariencia y sonidos

Elige un estilo de iconos (**Classic, Outline, Pixel**). Cada fila muestra una vista previa; **Integrado** restaura el estilo. En **Sonidos → Fuentes de sonido**, selecciona para cada estado un aviso de Windows, tu archivo WAV/MP3, silencio o el estilo elegido. Los botones de prueba no cambian el estado del micrófono.

Para cada estado puedes elegir un icono `.ico`, una imagen `.png`, `.bmp`, `.jpg`, `.jpeg` o `.ico`, y un sonido `.wav` o `.mp3`. Si un archivo no se puede usar, se aplica el estilo elegido. Los MP3 son archivos locales: Windows MCI los reproduce y cierra el dispositivo de reproducción al terminar, sin códecs adicionales incluidos.

La configuración se guarda en `MicMuteConfig.txt` junto al programa, con copia `.bak`. El desinstalador conserva la configuración y los archivos creados por el usuario. Guarda tus archivos personalizados fuera de `assets/default`. No compartas tu configuración ni tus copias: pueden contener identificadores de dispositivos y rutas locales.

MicMute no graba ni transmite audio ni recopila telemetría. Solo consulta GitHub para las actualizaciones, sin enviar tus preferencias, rutas de archivos ni datos del micrófono. Cambia el estado de silencio del dispositivo en Windows, no el de una aplicación concreta. El indicador no está garantizado sobre aplicaciones en pantalla completa exclusiva o el escritorio seguro de UAC.

Consulta la [guía de compilación](README.md#build-from-source), la [licencia GPL-3.0-or-later](LICENSE) y los [créditos](THIRD-PARTY-NOTICES.md).

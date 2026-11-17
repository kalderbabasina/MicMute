# MicMute 3.1.0

Una utilidad para silenciar el micrófono en Windows desde la bandeja, un indicador en pantalla o un atajo global. Usa WinForms y .NET Framework 4.8, sin paquetes NuGet.

[English README](README.md) · [Русская инструкция](README.ru.md)

[Descargar instalador](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-Setup.exe) · [ZIP portable](https://github.com/kalderbabasina/MicMute/releases/latest/download/MicMute-portable.zip) · [Versiones y SHA-256](https://github.com/kalderbabasina/MicMute/releases)

## Instalación y uso

Necesitas Windows 10 o posterior y .NET Framework 4.8 o posterior. Ejecuta `MicMute-Setup.exe` para instalarlo sin permisos de administrador, o descomprime `MicMute-portable.zip` en una carpeta con permisos de escritura y abre `MicMute2.exe`. Los ejecutables no tienen firma digital; comprueba las sumas SHA-256 publicadas junto a la descarga.

- En el primer inicio se abre la configuración. El idioma inicial es inglés; elige español en **General → Language**.
- Selecciona el micrófono predeterminado para comunicaciones de Windows o un dispositivo de captura concreto.
- Pulsa **F13** (atajo predeterminado) o haz clic en el icono de la bandeja o en el indicador para silenciar o reactivar el micrófono. Puedes cambiar el atajo en la configuración.
- Arrastra el indicador para moverlo, o mantén pulsada **Mayús** mientras lo arrastras para cambiar el tamaño (40–256 px). Puedes ocultarlo, bloquearlo y ajustar su opacidad. Un **?** gris indica que no se pudo leer el estado, no que el micrófono esté silenciado.

## Apariencia y sonidos

Elige un estilo de iconos (**Classic, Outline, Pixel**) y otro de sonidos (**Windows, Soft, Arcade**). La vista previa de iconos muestra los dos estados integrados. Los botones de prueba de sonido reproducen el aviso de cada estado sin cambiar el micrófono.

Para cada estado puedes elegir un icono `.ico`, una imagen `.png`, `.bmp`, `.jpg`, `.jpeg` o `.ico`, y un sonido `.wav` o `.mp3`. Si un archivo no se puede usar, se aplica el estilo elegido. Los MP3 son archivos locales: Windows MCI los reproduce y cierra el dispositivo de reproducción al terminar, sin códecs adicionales incluidos.

La configuración se guarda en `MicMuteConfig.txt` junto al programa, con copia `.bak`. El desinstalador conserva la configuración y los archivos creados por el usuario. Guarda tus archivos personalizados fuera de `assets/default`. No compartas tu configuración ni tus copias: pueden contener identificadores de dispositivos y rutas locales.

MicMute no graba ni transmite audio ni recopila telemetría. Cambia el estado de silencio del dispositivo en Windows, no el de una aplicación concreta. El indicador no está garantizado sobre aplicaciones en pantalla completa exclusiva o el escritorio seguro de UAC.

Consulta la [guía de compilación](README.md#build-from-source), la [licencia GPL-3.0-or-later](LICENSE) y los [créditos](THIRD-PARTY-NOTICES.md).

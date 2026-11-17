using System;
using System.Collections.Generic;

namespace MicMute
{
    internal static class UiText
    {
        // Russian source phrases are also their lookup keys; each pair is English, Spanish.
        private static readonly Dictionary<string, string[]> Translations = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "MicMute — настройки", new[] { "MicMute — Settings", "MicMute — Configuración" } },
            { "Отмена", new[] { "Cancel", "Cancelar" } },
            { "ОК", new[] { "OK", "Aceptar" } },
            { "Основное", new[] { "General", "General" } },
            { "Язык", new[] { "Language", "Idioma" } },
            { "Микрофон", new[] { "Microphone", "Micrófono" } },
            { "Обновить", new[] { "Refresh", "Actualizar" } },
            { "Включить глобальную горячую клавишу", new[] { "Enable global hotkey", "Activar atajo global" } },
            { "Горячая клавиша: нажмите сочетание", new[] { "Hotkey: press a key combination", "Atajo: pulsa una combinación de teclas" } },
            { "Нажмите клавишу или сочетание с Ctrl, Alt, Shift. Tab — переход, Esc — отмена, Delete / Backspace — очистить.",
                new[] { "Press a key, optionally with Ctrl, Alt or Shift. Tab to move on, Esc to cancel, Delete or Backspace to clear.",
                    "Pulsa una tecla, sola o con Ctrl, Alt o Mayús. Tab para avanzar, Esc para cancelar, Supr o Retroceso para borrar." } },
            { "Горячая клавиша", new[] { "Hotkey", "Atajo de teclado" } },
            { "Показывать плавающий индикатор", new[] { "Show floating indicator", "Mostrar indicador flotante" } },
            { "Размер, пиксели", new[] { "Size, pixels", "Tamaño, píxeles" } },
            { "Размер индикатора в пикселях", new[] { "Indicator size in pixels", "Tamaño del indicador en píxeles" } },
            { "Непрозрачность, %", new[] { "Opacity, %", "Opacidad, %" } },
            { "Непрозрачность индикатора", new[] { "Indicator opacity", "Opacidad del indicador" } },
            { "Непрозрачность индикатора в процентах", new[] { "Indicator opacity as a percentage", "Opacidad del indicador en porcentaje" } },
            { "Заблокировать положение и размер", new[] { "Lock position and size", "Bloquear posición y tamaño" } },
            { "Сбросить позицию", new[] { "Reset position", "Restablecer posición" } },
            { "Позиция будет сброшена после нажатия ОК.", new[] { "The position will reset when you select OK.", "La posición se restablecerá al pulsar Aceptar." } },
            { "Перетаскивание — перемещение; Shift + перетаскивание — изменение размера. Блокировка отключает оба действия.",
                new[] { "Drag to move; Shift + drag to resize. Locking disables both actions.",
                    "Arrastra para mover; Mayús + arrastrar para cambiar el tamaño. El bloqueo desactiva ambas acciones." } },
            { "Индикатор", new[] { "Indicator", "Indicador" } },
            { "Внешний вид", new[] { "Appearance", "Apariencia" } },
            { "Стиль значков", new[] { "Icon style", "Estilo de iconos" } },
            { "Классический", new[] { "Classic", "Clásico" } },
            { "Контурный", new[] { "Outline", "Contorno" } },
            { "Пиксельный", new[] { "Pixel", "Píxeles" } },
            { "Пустое поле — встроенное оформление. Файлы не копируются: храните их по выбранному пути.",
                new[] { "An empty field uses the built-in style. Files are not copied; leave them at the selected path.",
                    "Un campo vacío usa el estilo integrado. Los archivos no se copian; déjalos en la ruta seleccionada." } },
            { "Включён", new[] { "Active", "Activo" } },
            { "Выключен", new[] { "Muted", "Silenciado" } },
            { "Значок в области уведомлений (ICO)", new[] { "Tray icon (ICO)", "Icono del área de notificación (ICO)" } },
            { "Плавающий индикатор (PNG, BMP, JPG, ICO)", new[] { "Floating indicator (PNG, BMP, JPG, ICO)", "Indicador flotante (PNG, BMP, JPG, ICO)" } },
            { "Ограничения: до 16 МБ на файл и до 4096 × 4096 пикселей. Для прозрачного фона используйте PNG или ICO.",
                new[] { "Up to 16 MB per file and 4096 × 4096 pixels. Use PNG or ICO for a transparent background.",
                    "Hasta 16 MB por archivo y 4096 × 4096 píxeles. Usa PNG o ICO para un fondo transparente." } },
            { "Звуки", new[] { "Sounds", "Sonidos" } },
            { "Воспроизводить звуки переключения", new[] { "Play sounds when muting or unmuting", "Reproducir sonidos al silenciar o reactivar" } },
            { "Также при изменениях из других приложений", new[] { "Also for changes made by other apps", "También con cambios de otras aplicaciones" } },
            { "Стиль звуков", new[] { "Sound style", "Estilo de sonido" } },
            { "Мягкий", new[] { "Soft", "Suave" } },
            { "Аркадный", new[] { "Arcade", "Arcade" } },
            { "Прослушать выключение", new[] { "Preview mute", "Escuchar al silenciar" } },
            { "Прослушать включение", new[] { "Preview unmute", "Escuchar al reactivar" } },
            { "Выключение", new[] { "Mute", "Silenciar" } },
            { "Включение", new[] { "Unmute", "Reactivar" } },
            { "Звуковые файлы (WAV, MP3)", new[] { "Sound files (WAV, MP3)", "Archivos de sonido (WAV, MP3)" } },
            { "Пустое поле — звук выбранного стиля. WAV и MP3: до 16 МБ на файл. Храните файлы по выбранному пути.",
                new[] { "An empty field uses the selected sound style. WAV and MP3: up to 16 MB per file. Leave files at the selected path.",
                    "Un campo vacío usa el estilo de sonido seleccionado. WAV y MP3: hasta 16 MB por archivo. Deja los archivos en la ruta seleccionada." } },
            { "По умолчанию (устройство связи)", new[] { "Default (communications device)", "Predeterminado (dispositivo de comunicaciones)" } },
            { "Не удалось получить список микрофонов: {0}", new[] { "Could not list microphones: {0}", "No se pudieron enumerar los micrófonos: {0}" } },
            { "Недоступен: {0}", new[] { "Unavailable: {0}", "No disponible: {0}" } },
            { "Не задана", new[] { "Not set", "Sin asignar" } },
            { "Задайте горячую клавишу или отключите её использование.", new[] { "Choose a hotkey or disable it.", "Elige un atajo o desactívalo." } },
            { "{0}: путь к файлу", new[] { "{0}: file path", "{0}: ruta del archivo" } },
            { "Выбрать…", new[] { "Browse…", "Examinar…" } },
            { "Сброс", new[] { "Reset", "Restablecer" } },
            { "{0}: выбрать файл", new[] { "{0}: browse for a file", "{0}: examinar archivos" } },
            { "{0}: сбросить файл", new[] { "{0}: reset file", "{0}: quitar archivo" } },
            { "Выберите файл — {0}", new[] { "Choose a file — {0}", "Selecciona un archivo — {0}" } },
            { "Значки ICO (*.ico)", new[] { "ICO icons (*.ico)", "Iconos ICO (*.ico)" } },
            { "Изображения (*.png;*.bmp;*.jpg;*.jpeg;*.ico)", new[] { "Images (*.png;*.bmp;*.jpg;*.jpeg;*.ico)", "Imágenes (*.png;*.bmp;*.jpg;*.jpeg;*.ico)" } },
            { "Не удалось выбрать файл.", new[] { "Could not select the file.", "No se pudo seleccionar el archivo." } },
            { "Неподходящий файл", new[] { "Invalid file", "Archivo no válido" } },
            { "Требуется читаемый файл WAV или MP3.", new[] { "Choose a readable WAV or MP3 file.", "Selecciona un archivo WAV o MP3 que se pueda leer." } },
            { "Максимальный размер изображения — 4096 × 4096 пикселей.",
                new[] { "The maximum image size is 4096 × 4096 pixels.", "El tamaño máximo de la imagen es de 4096 × 4096 píxeles." } },
            { "Поддерживаются только PNG, BMP, JPG и ICO.", new[] { "Only PNG, BMP, JPG and ICO are supported.", "Solo se admiten PNG, BMP, JPG e ICO." } },
            { "Размер файла должен быть от 1 байта до 16 МБ.", new[] { "The file must be between 1 byte and 16 MB.", "El archivo debe tener entre 1 byte y 16 MB." } },

            { "MicMute уже запущен. Откройте настройки через значок в трее или на экране.",
                new[] { "MicMute is already running. Open Settings from the tray icon or the floating indicator.",
                    "MicMute ya está en ejecución. Abre Configuración desde el icono del área de notificación o el indicador flotante." } },
            { "Не удалось запустить MicMute.\n", new[] { "Could not start MicMute.\n", "No se pudo iniciar MicMute.\n" } },
            { "Переключить микрофон", new[] { "Toggle microphone", "Silenciar o reactivar el micrófono" } },
            { "Настройки…", new[] { "Settings…", "Configuración…" } },
            { "Выход", new[] { "Exit", "Salir" } },
            { "Горячая клавиша недоступна. Выберите другую в настройках.",
                new[] { "The hotkey is unavailable. Choose a different one in Settings.",
                    "El atajo no está disponible. Elige otro en Configuración." } },
            { "MicMute — микрофон недоступен", new[] { "MicMute — microphone unavailable", "MicMute — micrófono no disponible" } },
            { "MicMute — микрофон выключен", new[] { "MicMute — microphone muted", "MicMute — micrófono silenciado" } },
            { "MicMute — микрофон включён", new[] { "MicMute — microphone active", "MicMute — micrófono activo" } },
            { "Включить микрофон", new[] { "Unmute microphone", "Reactivar el micrófono" } },
            { "Выключить микрофон", new[] { "Mute microphone", "Silenciar el micrófono" } },
            { "Микрофон недоступен. Подключите его или выберите устройство в настройках.",
                new[] { "The microphone is unavailable. Connect it or select another device in Settings.",
                    "El micrófono no está disponible. Conéctalo o selecciona otro dispositivo en Configuración." } },
            { "Windows не подтвердила переключение микрофона. Проверьте выбранное устройство.",
                new[] { "Windows did not confirm the microphone change. Check the selected device.",
                    "Windows no confirmó el cambio del micrófono. Comprueba el dispositivo seleccionado." } },
            { "Эта горячая клавиша уже занята или не поддерживается. Выберите другую либо отключите её.",
                new[] { "This hotkey is already in use or is not supported. Choose a different one or disable it.",
                    "Este atajo ya está en uso o no es compatible. Elige otro o desactívalo." } },
            { "Не удалось сохранить настройки.\n", new[] { "Could not save settings.\n", "No se pudo guardar la configuración.\n" } },
            { "Не удалось сохранить позицию. Проверьте доступ к папке программы.",
                new[] { "Could not save the position. Check write access to the application folder.",
                    "No se pudo guardar la posición. Comprueba el permiso de escritura en la carpeta de la aplicación." } },
            { "Индикатор MicMute", new[] { "MicMute indicator", "Indicador de MicMute" } },
            { "Состояние микрофона", new[] { "Microphone status", "Estado del micrófono" } },
            { "Микрофон недоступен", new[] { "Microphone unavailable", "Micrófono no disponible" } },
            { "Микрофон выключен", new[] { "Microphone muted", "Micrófono silenciado" } },
            { "Микрофон включён", new[] { "Microphone active", "Micrófono activo" } },
            { "\nКлик — mute/unmute · Shift + перетаскивание — размер\nПравая кнопка — настройки",
                new[] { "\nClick to mute/unmute · Shift + drag to resize\nRight-click to open the menu",
                    "\nClic para silenciar o reactivar · Mayús + arrastrar para cambiar el tamaño\nClic derecho para abrir el menú" } },
        };

        internal static string NormalizeLanguage(string language)
        {
            string value = (language ?? String.Empty).Trim().ToLowerInvariant();
            return value == "ru" || value == "es" ? value : "en";
        }

        internal static string Get(string russianKey, string language)
        {
            string value = NormalizeLanguage(language);
            string[] translations;
            if (value == "ru" || !Translations.TryGetValue(russianKey, out translations)) return russianKey;
            return translations[value == "es" ? 1 : 0];
        }
    }
}

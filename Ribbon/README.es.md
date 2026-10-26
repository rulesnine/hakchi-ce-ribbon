# Hakchi CE Ribbon 1.0

Cinta de herramientas para Hakchi CE **3.9.3** en Windows / .NET Framework 4.6.1.

## Actualizar la versión portátil

1. Cierra todas las ventanas de Hakchi, incluidos los diálogos de módulos.
2. Copia `hakchi-ribbon.exe` y `hakchi-ribbon.exe.config` junto al `hakchi.exe` original.
3. Abre `hakchi-ribbon.exe`. Desde entonces usa ese acceso para ver la cinta.

En esta laptop la carpeta es `C:\NES hack\hakchi2-ce-3.9.3-portable`.

El lanzador carga el ejecutable original en el mismo proceso y agrega la cinta
cuando aparece su ventana principal. No modifica `hakchi.exe`, juegos, módulos,
configuración ni firmware. No puede añadir la cinta a un proceso ya abierto.
Para volver a la interfaz original basta con abrir `hakchi.exe`.

## Interfaz

- Inicio: añadir juegos, carpetas, sincronizar, exportar, partidas, explorador.
- Juegos: biblioteca, emulador, carátulas e información.
- Módulos: instalar, desinstalar, repositorios e informe.
- Consola: explorador, FTP, Telnet, partidas y captura.

Los accesos llaman a los controles originales con `PerformClick`, conservando
sus validaciones y confirmaciones. Se actualizan al cambiar su disponibilidad.
`View/Ver > Cinta de herramientas` permite ocultarla durante la sesión.
Las etiquetas de la cinta están en español; los menús originales mantienen el idioma configurado.
En ventanas estrechas las herramientas se desplazan horizontalmente.

## Compilar el lanzador

Desde PowerShell: `powershell -ExecutionPolicy Bypass -File Ribbon\Build.ps1`.
Utiliza el compilador de .NET Framework incluido en Windows, sin paquetes externos.
El archivo de configuración conserva los enlaces de ensamblados de Hakchi.

La compilación completa de `hakchi_gui.csproj` incluye la misma cinta de forma nativa.
Para esa compilación se necesitan las herramientas y dependencias del proyecto original.
`Launcher.cs` no forma parte del ejecutable principal.

## Origen y licencia

Basado en TeamShinkansen/Hakchi2-CE, etiqueta v3.9.3,
commit 25bfcfd04b8ed12f7d513dc87c2a0f7096d38ebc.
Se conserva la licencia GPL del proyecto (ver LICENSE).
Los archivos fuente del lanzador y de la cinta se distribuyen bajo esa misma licencia.

## Carátulas en Ribbon 1.1

El botón Google se convierte en Carátulas. Consulta Named_Boxarts de
https://thumbnails.libretro.com por nombre y consola, muestra una vista previa
y guarda la elección mediante la propiedad Image original de Hakchi.
No requiere cuenta ni claves de API. La búsqueda de Google dentro del control
antiguo ya no se utiliza para este botón. Google (navegador) abre la búsqueda
en el navegador habitual; una imagen puede importarse por URL directa o archivo.
No modifica la búsqueda automática de carátulas por lotes del menú contextual.

Se comprobaron descargas reales de NES y Game Boy, el selector completo, el
arranque de WinForms y el guardado de imagen/metadatos en una biblioteca de prueba.
Los catálogos dependen de la disponibilidad de Libretro y de que el juego exista
en la colección. Puede buscarse con un nombre más corto o elegir otra consola.

Pruebas: CoverTests.cs (requiere internet), StartupTests.cs y Tests.cs.
Se compilan con csc de .NET Framework, incluyendo CoverArt.cs y los archivos
que utilice cada prueba. Seleccionar /main:CoverTests, /main:StartupTests o /main:RibbonTests.

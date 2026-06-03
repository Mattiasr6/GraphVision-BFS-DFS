# Graph Vision — BFS & DFS

**Estructura de Datos | Proyecto Final**

Aplicación interactiva en **C# Windows Forms (.NET 8)** que implementa un **grafo no dirigido** con visualización en tiempo real de los algoritmos de recorrido **BFS (Búsqueda en Anchura)** y **DFS (Búsqueda en Profundidad)**.

---

## ✨ Funcionalidades

### 🧠 Algoritmos de recorrido
- **BFS (Breadth-First Search)** — recorrido por niveles mediante cola FIFO. Muestra el estado de la cola en cada paso.
- **DFS Iterativo** — recorrido en profundidad con pila explícita LIFO. Detecta aristas de retroceso (*back edges*) para identificar ciclos.
- **DFS Recursivo** — versión clásica con visualización de la pila de llamadas simulada y *backtracking*.

### 🎮 Editor interactivo de grafos
| Acción | Cómo |
|---|---|
| **Agregar nodo** | Click en espacio vacío del canvas |
| **Conectar nodos** | Click en nodo A → Click en nodo B |
| **Mover nodo** | Arrastrarlo con el mouse |
| **Eliminar nodo** | Click derecho sobre el nodo |
| **Seleccionar origen** | Doble click sobre un nodo |

### 🎬 Animación automática
- Presione **Ejecutar** → el algoritmo se reproduce automáticamente en tiempo real.
- **Pausa/Reanudar** con botón o barra espaciadora.
- **Velocidad ajustable**: 0.5×, 1×, 2×, 4×.
- **Navegación manual**: botones Anterior / Siguiente para revisar paso a paso.

### 📐 Layouts de visualización
- **Circular** — nodos distribuidos uniformemente en un círculo.
- **Árbol** — disposición jerárquica por niveles BFS.
- **Force-Directed** — algoritmo de Fruchterman-Reingold que minimiza cruces de aristas.

### 🎨 Interfaz profesional
- Tema oscuro moderno con paleta de colores neón.
- **Panel de estructura interna** (cola/pila) con estilo 3D y flechas direccionales.
- **Zoom** con rueda del ratón (0.2× a 5×).
- **Pan** con arrastre de botón central.
- **Glow pulsante** en el nodo activo durante la animación.
- Cursor *hand* al pasar sobre nodos.
- Resaltado de aristas: **celeste** (árbol de expansión), **magenta punteado** (retroceso/ciclos).

---

## 🖼️ Capturas de pantalla

*(Insertar aquí las capturas de la aplicación en funcionamiento)*

---

## 📋 Requisitos del sistema

- **Sistema operativo**: Windows 10 / 11
- **.NET SDK**: 8.0 o superior ([Descargar](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Windows Desktop Runtime**: 8.0 (incluido con el SDK)
- **Visual Studio** (opcional): 2022+ o Visual Studio Insiders 2026

---

## 🚀 Cómo ejecutar

### Opción 1 — Terminal (recomendado)

```powershell
dotnet run
```

### Opción 2 — Visual Studio

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\devenv.exe" "GraphTraversal.csproj"
```

Luego presione **`F5`** (Debug) o **`Ctrl+F5`** (sin debug).

### Opción 3 — Ejecutable directo

```powershell
.\bin\Debug\net8.0-windows\GraphTraversal.exe
```

---

## 🏗️ Estructura del proyecto

```
ProyectoFinal/
├── Models/
│   ├── Graph.cs              — Clase principal del grafo (nodos, aristas, layouts)
│   ├── GraphNode.cs          — Representación de un vértice
│   └── TraversalResult.cs    — Record inmutable con el resultado del algoritmo
├── Algorithms/
│   ├── BFS.cs                — BFS con captura de snapshots de la cola
│   └── DFS.cs                — DFS iterativo y recursivo con detección de ciclos
├── Forms/
│   └── MainForm.cs           — Interfaz gráfica completa (~960 líneas)
├── Utils/
│   └── GraphGenerator.cs     — Generador de grafos de ejemplo y algoritmos de layout
├── Program.cs                — Punto de entrada de la aplicación
├── GraphTraversal.csproj     — Archivo de proyecto .NET 8.0 (Windows Forms)
└── README.md                 — Este archivo
```

---

## 🛠️ Tecnologías utilizadas

| Tecnología | Versión |
|---|---|
| C# | 12 (.NET 8.0) |
| Windows Forms | — |
| .NET SDK | 8.0.x |
| Target Framework | `net8.0-windows` |

---

## 📄 Documentación académica

El proyecto incluye un informe técnico completo en formato Word:

- **`Informe_Proyecto.docx`** — Documento académico con introducción, marco teórico, desarrollo, diagnóstico (40 %), pronóstico, recomendaciones, conclusión, bibliografía y anexos.

---

## 📊 Resultados de pruebas

| Configuración | Nodos | Aristas | Algoritmo | Visitados | Ciclos | Tiempo |
|---|---|---|---|---|---|---|
| Árbol | 8 | 7 | BFS / DFS | 8/8 | 0 | <1 ms |
| Grafo con ciclos | 9 | 12 | BFS | 9/9 | — | <1 ms |
| Grafo con ciclos | 9 | 12 | DFS | 9/9 | 2 | <1 ms |
| Aleatorio (10) | 10 | ~16 | BFS / DFS | 10/10 | — | <1 ms |
| Aleatorio (15) | 15 | ~26 | BFS / DFS | 15/15 | — | <2 ms |

---

## 📚 Referencias

- Cormen, T. H. et al. (2009). *Introduction to Algorithms* (3rd ed.). MIT Press.
- Sedgewick, R. & Wayne, K. (2011). *Algorithms* (4th ed.). Addison-Wesley.
- Fruchterman, T. M. J. & Reingold, E. M. (1991). *Graph Drawing by Force-Directed Placement*. SPE, 21(11), 1129–1164.

---

## 📝 Licencia

Proyecto académico — Estructura de Datos.

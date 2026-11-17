# 🧺 Lavandería Multijugador

**Materia:** Desarrollo de Videojuegos Multiplayer con Unity3D  
**Motor:** Unity 6 (Universal 3D)  
**Red:** Unity Netcode for GameObjects (NGO) + Unity Relay (Conexión P2P)

## 📌 Descripción General
Es un juego cooperativo donde los jugadores gestionan una lavandería. Los NPCs llegan con pedidos de ropa sucia, los jugadores deben lavarla con los parámetros correctos, secarla, clasificarla y entregarla para ganar dinero y comprar mejoras antes de que termine el día.

---

## 🏗️ Arquitectura de Red (Server-Authoritative)
El proyecto está diseñado bajo el paradigma **Server-Authoritative (Autoridad en el Servidor)**. El servidor (Host) tiene el control total de la lógica y el estado. Los clientes actúan como terminales de input y renderizado local, sin poder alterar el estado global de forma directa.

- **RPCs (Remote Procedure Calls):** Los clientes envían sus intenciones al servidor mediante `[Rpc(SendTo.Server)]`. El servidor valida la acción (distancia, pertenencia, saldo, etc.) antes de aplicar cambios.
- **NetworkVariables (NV):** Utilizadas para la sincronización de estado persistente en tiempo real. Todas las NV críticas tienen permisos de escritura exclusivos para el servidor (`NetworkVariableWritePermission.Server`).
- **ClientRpc:** Utilizados exclusivamente para feedback visual (ej. mostrar/ocultar la malla de la ropa en todos los clientes).

---

## 🧩 Módulos y Scripts Principales

### 👥 Gestión de Jugadores y Red
- **`MenuConexion`:** Utiliza `Unity.Services.Relay` para generar un código de sala. El Host crea la `Allocation` y el Cliente se une con el código.
- **`PlayerMovement`:** Controla el movimiento con `CharacterController`. La rotación de la cámara se sincroniza mediante la `NetworkVariable` `rotacionXRed`.
- **`PlayerSpawn`:** Garantiza que el jugador spawnee en el punto correcto. Usa una corrutina para esperar un frame (evitando problemas con la física del `CharacterController`).
- **`PlayerInventory`:** Lógica central del inventario. El objeto real se oculta en el mundo y se activa un *mesh* falso en las manos del jugador.

### 🧼 Máquinas y Lavandería
- **`MaquinaNetwork`:** Controla la lógica de entrada y salida de ropa. Evalúa si la ropa coincide con los requisitos, inicia corrutinas de tiempo (`RutinaTrabajoNet`) y calcula si los modos usados fueron correctos.
- **`MaquinaLogica`:** Separación de responsabilidades (patrón Mediator). Se encarga exclusivamente de la UI local de la máquina sin saber nada de red.
- **`LavanderiaManager`:** La "Fábrica" de ropa. Recibe parámetros de las máquinas e instancia el prefab correcto (`RopaMojada`, `RopaSeca` o `RopaArruinada`).

### 🛒 NPCs y Economía
- **`NPCCliente`:** La IA del cliente con `NavMeshAgent`. Valida la entrega en el servidor: si la ropa está seca y es correcta, paga en `DineroManager`; si está arruinada, no paga. Lleva un conteo de bolsas entregadas.
- **`DineroManager`:** Centraliza el dinero compartido del equipo mediante una `NetworkVariable<int>`.
- **`ColaManager`:** Gestiona la fila de NPCs. Escala la dificultad día a día (aumentando la velocidad de spawn y cantidad máxima de clientes).
- **`MostradorManager`:** Controla los puntos físicos donde el NPC deposita los bolsos y asigna los apodos a los paquetes.

### 🔄 Progresión y UI
- **`DiaManager`:** Simula el ciclo día/noche, controlando la hora (`NetworkVariables`) y la iluminación global del sol. Al cerrar el día, muestra la pantalla de resumen.
- **`TablonNetwork` / `TablonLogica`:** Implementan el sistema de compras (Patrón Mediator). El servidor valida el saldo antes de restar dinero y spawnear la nueva máquina.
- **`PlayerUI`:** Toda la interfaz es local (Regla de Oro del multijugador). Escucha las `OnValueChanged` de las NetworkVariables para actualizar dinero, hora, notas y la lista de jugadores conectados.

---

## 🛠️ Resolución de Problemas de Red (Clase 12 y 13)

- **UI Local:** No se envían UIs por la red. Cada cliente actualiza su dinero, hora y lista leyendo las `NetworkVariables`.
- **Efectos Físicos:** El ocultamiento y mostrado de la ropa se hace con `[ClientRpc]` para asegurar que todos vean la misma transición.
- **Manejo de Desconexiones:** 
  - *Cliente se va:* El `OnNetworkDespawn` del `PlayerInventory` tira la ropa al piso.
  - *Host se va:* El `AlDesconectarse` del `PlayerUI` fuerza el `Shutdown()`, libera el cursor y devuelve al cliente al Menú Principal.
- **Spawn Seguro:** El `PlayerSpawn` espera un frame antes de mover al jugador para no atravesar el piso con el `CharacterController`.

---

## 🎮 Controles
- **WASD:** Movimiento.
- **Mouse:** Rotar cámara.
- **Click Izquierdo:** Interactuar (Agarrar ropa, máquinas, entregar NPC, compras).
- **Tecla Q / Soltar:** Soltar objeto.
- **Tab / Escape:** Menú de opciones (Dinero, lista de jugadores, salir).

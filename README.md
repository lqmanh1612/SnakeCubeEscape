# Arrow Cube Escape - Unity 3D Puzzle Game

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity](https://img.shields.io/badge/Unity-2022.3+-blue.svg)](https://unity.com/)

[Tiếng Việt](#tiếng-việt) | [English](#english)

---

## Tiếng Việt

**Arrow Cube Escape** là một trò chơi giải đố 3D mã nguồn mở được phát triển bằng Unity. Người chơi cần giải phóng tất cả các mũi tên bám trên các mặt của một khối lập phương bằng cách xoay khối và đẩy các mũi tên thoát ra ngoài không gian.

### 🌟 Tính năng kỹ thuật nổi bật
*   **Hệ thống Va chạm Xuyên mặt (Cross-face Collision):** Xử lý logic va chạm phức tạp khi mũi tên uốn lượn qua nhiều mặt của khối lập phương 3D.
*   **Hệ thống Sinh Level Động (Dynamic Level Generator):** Tự động khởi tạo màn chơi từ dữ liệu ScriptableObject thay vì bake scene thủ công, giúp tối ưu hiệu suất và dễ dàng mở rộng.
*   **Kiến trúc Dữ liệu Phân tầng:** Sử dụng `LevelDatabase` để quản lý tiến trình người chơi và lưu trữ ScriptableObject.
*   **Giao diện Responsive:** Cấu hình UI linh hoạt cho nhiều tỷ lệ màn hình khác nhau (Mobile/PC).

### 🛠 Tech Stack
*   **Engine:** Unity 3D
*   **Ngôn ngữ:** C#
*   **Thư viện:** DOTween (Animation & Tweening), TextMeshPro (UI).

### 🎮 Cách chơi
1. **Xoay khối:** Click và kéo chuột (hoặc swipe) để xoay khối lập phương.
2. **Đẩy mũi tên:** Click vào mũi tên để nó trượt đi. Mũi tên chỉ thoát được khi đường đi phía trước không có vật cản.
3. **Mục tiêu:** Giải phóng toàn bộ mũi tên để qua màn.

---

## English

**Arrow Cube Escape** is an open-source 3D puzzle game built with Unity. The goal is to release all arrows attached to the faces of a cube by rotating the cube and sliding arrows out into space.

### 🌟 Technical Highlights
*   **Cross-face Collision Logic:** Handles complex collision detection for arrows that wrap across multiple 3D cube faces using custom topology mapping.
*   **Dynamic Level Generation:** Levels are instantiated at runtime from ScriptableObject data, ensuring scalability and efficient scene management.
*   **Data-Driven Architecture:** Utilizes `LevelDatabase` for progression management and centralized asset control.
*   **Responsive UI:** Optimized Canvas scaling for various aspect ratios and resolutions.

### 🛠 Tech Stack
*   **Engine:** Unity 3D
*   **Language:** C#
*   **Plugins:** DOTween (Animations), TextMeshPro.

### 🕹 Controls
1. **Rotate Cube:** Drag the mouse/swipe to rotate.
2. **Slide Arrow:** Click on an arrow. It will only slide out if its path is clear of obstacles.
3. **Objective:** Clear all arrows to advance to the next level.

---

## 📂 Project Structure
*   `Assets/Scripts/GameManager.cs`: Main game loop and state management.
*   `Assets/Scripts/ArrowTile.cs`: Individual arrow logic and movement.
*   `Assets/Scripts/LevelGenerator.cs`: Runtime level instantiation logic.
*   `Assets/Scripts/CubeTopology.cs`: Handles face-to-face coordinate mapping.

---
*Developed as a portfolio project for Game Development.*

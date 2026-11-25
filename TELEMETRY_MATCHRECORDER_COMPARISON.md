# Đối chiếu telemetry với `MatchRecorder` của Slime-TD

## Phạm vi

- Tài liệu yêu cầu: `data_collection_and_dataset_building.md` (project Slime-TD).
- Repo GitHub: [hadayne07/Slime-TD](https://github.com/hadayne07/Slime-TD), clone để kiểm tra tại commit `3f7f84e`.
- Script hiện có trước thay đổi: `Assets/_Scenes/Script/MatchRecorder.cs`.
- Unity project version: `6000.3.23f1`.

## 1. Hai mẫu snapshot được trích từ tài liệu

### Snapshot A — ví dụ thiết kế ở mục 3.2

```json
{
  "schemaVersion": "telemetry-v1",
  "sessionId": "session-001",
  "playerIdHash": "hmac-...",
  "recordedAtUtc": "2026-11-23T10:00:05Z",
  "elapsedRealSeconds": 65.0,
  "segmentId": "wave-03",
  "objectiveId": "defend_core",
  "currentDifficulty": 2,
  "progressRatio": 0.34,
  "hp": 82,
  "hpMax": 100,
  "gold": 240,
  "towerCount": 5,
  "wave": 3,
  "enemyAlive": 12,
  "kills5s": 4,
  "hitsTaken5s": 1,
  "hitsTaken30s": 6,
  "deaths30s": 0,
  "enemyLeaks30s": 2,
  "actionCount5s": 8,
  "invalidActionRate": 0.0,
  "idleRatio5s": 0.12,
  "scoreEma": 0.61
}
```

### Snapshot B — raw record ví dụ ở mục 7.2

```json
{
  "sessionId": "s-001",
  "recordedAtUtc": "2026-11-23T10:01:05Z",
  "elapsedRealSeconds": 65.0,
  "segmentId": "wave-03",
  "objectiveId": "defend_core",
  "currentDifficulty": 2,
  "hp": 82,
  "hpMax": 100,
  "gold": 240,
  "towerCount": 5,
  "wave": 3,
  "progressRatio": 0.34,
  "enemyAlive": 12,
  "kills5s": 4,
  "hitsTaken5s": 1,
  "hitsTaken30s": 6,
  "deaths30s": 0,
  "enemyLeaks30s": 2,
  "actionCount5s": 8,
  "invalidActionRate": 0.0,
  "idleRatio5s": 0.12,
  "scoreEma": 0.61
}
```

Hai ví dụ dùng cùng bộ metric gameplay. Snapshot B không lặp `schemaVersion` và `playerIdHash`, nhưng schema chung ở mục 3.0 yêu cầu các trường định danh này hoặc phải mô tả rõ nếu cố ý thiếu. `playerIdHash` chưa thể tạo trong prototype hiện tại; không nên dùng tên thật/email hoặc hash trần làm thay thế.

## 2. Event log được lọc từ mục 3.1

| Event trong tài liệu | Payload chính được yêu cầu |
|---|---|
| `session_start` | build, map, patch, device class |
| `segment_start` | segment, objective, difficulty đang áp dụng |
| `difficulty_applied` | profile/vector, policy, propensity |
| `enemy_spawn` | loại, số lượng, vị trí dạng bucket |
| `enemy_kill` | loại enemy, attacker, thời điểm |
| `player_hit` | damage, nguồn, HP sau hit |
| `tower_build` / `ability_used` | loại đối tượng, cost, vị trí dạng bucket |
| `invalid_action` | loại action, lý do |
| `objective_success` | segment, thời điểm hoàn tất |
| `objective_failure` | segment, lý do thất bại |
| `death` | segment, thời điểm |
| `quit` | segment, lý do nếu có |
| `crash` / `disconnect` | segment, nhóm lỗi |
| `segment_end` | outcome, thời điểm kết thúc, terminal flag |
| `session_end` | lý do, tổng thời lượng |

Tài liệu cũng yêu cầu các dòng event có schema/session/player/run/match/map/patch/timestamp/elapsed/segment/objective để có thể join và audit. Không được ghi PII.

## 3. `MatchRecorder` đã có gì?

`MatchRecorder` hiện ghi **một file JSON cho cả trận** trong `Application.persistentDataPath/MatchLogs/`. `MatchData` có `matchId`, `sceneName`, thời điểm bắt đầu/kết thúc, `result` (`InProgress`, `Won`, `Lost`, `Left`, `Quit`) và danh sách `samples`. Mỗi sample được ghi lúc bắt đầu, theo chu kỳ mặc định 30 giây, kết thúc, hoặc app pause.

| Trường/nhóm | Trạng thái trước khi thêm event logger | Nhận xét |
|---|---|---|
| session/run/match ID | Có `matchId` | Đã đổi để dùng chung ID với event log. |
| UTC timestamp + elapsed | Có trong sample | `elapsedRealSeconds`, `recordedAtUtc`; có thêm `combatSeconds`. |
| segment/wave | Có `wave`, `waveStarted`, `waveRunning` | Chưa có `segmentId`/`objectiveId`; hiện có thể suy ra wave nhưng không nên coi là ID contract. |
| HP, gold, tower count | Có | Snapshot tại các lần capture. |
| enemy leak | Một phần | Chỉ `enemiesLeaked` tích lũy từ đầu trận; chưa có cửa sổ 5/30 giây. |
| result cuối trận | Có | Chỉ kết quả trận, không căn chỉnh thành nhãn cho từng snapshot/window. |
| event rời rạc | **Không có** | `samples` là snapshot thưa, không phải event log. |
| map/build/patch/device metadata | Một phần | Có `sceneName`; không có `patchId`, `clientBuild`, `deviceClass`. |
| player hash | **Không có** | Không có định danh người chơi giả danh hóa. |
| HP max / tiến độ / enemy alive | **Không có** | GameManager không cung cấp HP tối đa; MatchRecorder cũng không đếm enemy đang sống/progress. |
| kills/hits/deaths theo cửa sổ | **Không có** | Cần bộ đếm theo cửa sổ nếu muốn khớp snapshot mẫu. |
| action count/invalid rate/idle/score EMA | **Không có** | Chưa được tính trong code gameplay. |
| difficulty, candidate, propensity, policy, seed | **Không có** | Prototype chưa có DDA/randomized assignment; tuyệt đối không điền giả. |
| file format | JSON nguyên trận | Khác raw `snapshots.jsonl`; event log mới được ghi riêng dạng JSONL. |

Trong các scene, component `MatchRecorder` được tìm thấy trong `Map1`. Event logger mới tự khởi tạo nên có thể ghi event ở scene `Map*` ngay cả khi `MatchRecorder` chưa được gắn ở scene đó. Vì thế hiện tại `Map2`/`Map3` có thể có event file nhưng không có snapshot JSON từ `MatchRecorder`; cần gắn recorder/triển khai snapshot tương đương nếu muốn đủ hai luồng.

## 4. Event nào đã được nối vào code sau thay đổi?

| Event | Tình trạng | Điểm tích hợp / giới hạn |
|---|---|---|
| `session_start` | Đã ghi | Bắt đầu khi vào scene tên `Map*`; có scene/map, `Application.version`, device class. |
| `segment_start` | Đã ghi | Khi bắt đầu mỗi wave; segment ID dạng `wave-01`, objective `defend_core`. |
| `difficulty_applied` | API sẵn sàng, chưa phát sinh | Có hàm hook cho controller tương lai. Game chưa có profile DDA, policy, propensity hay random seed. |
| `enemy_spawn` | Đã ghi | Loại enemy, count=1, wave, vị trí thô được lượng tử hóa thành bucket 2 world units. |
| `enemy_kill` | Đã ghi một phần | Loại enemy có; attacker đang ghi `unknown` vì đường projectile/damage chưa truyền nguồn đánh vào `EnemyStatus`. |
| `player_hit` | Đã ghi | Damage và HP sau khi enemy lọt đường đánh lõi; source ghi `enemy_leak`. |
| `tower_build` | Đã ghi | Loại TowerData, cost, vị trí bucket. |
| `tower_upgrade` | Đã ghi thêm | Event bổ sung cho nghiên cứu hành vi; cost lấy theo upgrade. |
| `ability_used` | Chưa có | Repo chưa thấy hệ thống ability. |
| `invalid_action` | Đã ghi một phần | Đặt tháp sai vị trí/thiếu vàng/thiếu prefab; nâng cấp tối đa/thiếu vàng. Không bao quát mọi thao tác UI. |
| `objective_success` / `objective_failure` | Đã ghi theo wave | Wave hoàn thành khi dọn hết enemy được gán outcome `won`; core về 0 HP được gán `lost`. Đây là quy tắc operational cho prototype, cần chốt lại trước thu thập chính thức. |
| `death` | Đã ghi | Ghi khi HP lõi về 0. |
| `segment_end` | Đã ghi | Khi wave kết thúc, chết, rời hoặc kết thúc ứng dụng; có raw outcome/terminal. |
| `quit` / `session_end` | Đã ghi | Qua nút thoát, quit ứng dụng, restart/về menu; restart bắt đầu file/session mới. |
| `crash` / `disconnect` | Chưa có | `OnApplicationQuit` không đảm bảo chạy khi crash/kill process; cần crash handler hoặc collector phía ngoài. |

## 5. Script và cách kiểm tra

Logger nằm ở [`Assets/_Scenes/Script/TelemetryEventLogger.cs`](Assets/_Scenes/Script/TelemetryEventLogger.cs). Script tự tạo `DontDestroyOnLoad` logger trước khi load scene; không cần kéo component vào từng scene. Các hook được thêm vào `EnemySpawn`, `EnemyStatus`, `GameManager`, `TowerPlacementManager`, `TowerShoot` và `UIController`. `MatchRecorder.matchId` dùng chung `TelemetryEventLogger.SessionId`, giúp ghép snapshot JSON với JSONL event theo `matchId`/`sessionId`.

File event được append từng dòng tại:

```text
Application.persistentDataPath/Telemetry/events_<UTC>_<sessionId>.jsonl
```

Mỗi dòng có `schemaVersion`, `eventId`, `eventType`, `sessionId`, `runId`, `matchId`, `mapId`, timestamp UTC, elapsed time, segment/objective, cùng payload và cờ `has...` cho các giá trị số tùy chọn. `playerIdHash` và `patchId` để null đến khi có cơ chế hợp lệ; không ghi định danh giả.

### Lưu ý trước khi thu thập NCKH

1. Chạy thử trong Unity Play Mode và kiểm tra thứ tự `session_start → segment_start → enemy_spawn/... → segment_end → session_end`.
2. Thêm snapshot định kỳ 5 giây nếu cần khớp tài liệu; `MatchRecorder` mặc định hiện là 30 giây.
3. Chưa thể tạo candidate-conditioned DDA dataset: game chưa áp dụng difficulty profile, chưa log propensity/seed, và thiếu nhiều hành vi snapshot.
4. Chốt semantics `objective_success` cho mỗi wave (ví dụ sống sót qua wave vs không có enemy lọt) trước khi diễn giải nhãn.
5. Cần test compile/playmode bằng Unity Editor. Môi trường chỉnh sửa này không có Unity Editor/.NET compiler để chạy build C#.

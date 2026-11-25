# Đối chiếu telemetry với `MatchRecorder` của Slime-TD

## Phạm vi

- Tài liệu schema: `data_collection_and_dataset_building.md` trong project Slime-TD.
- Repository: [hadayne07/Slime-TD](https://github.com/hadayne07/Slime-TD), nhánh làm việc `test/telemetry-event-log`.
- Unity: `6000.3.23f1`.
- Mục tiêu thay đổi: căn chỉnh snapshot/event gần nhất có thể với schema nghiên cứu; không giả lập các biến game chưa có nguồn dữ liệu.

## 1. Hai mẫu snapshot trong tài liệu

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

### Snapshot B — raw record ở mục 7.2

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

Hai mẫu dùng cùng bộ metric. `playerIdHash` và `patchId` không thể được điền hợp lệ ở prototype này, nên logger để `null`; không dùng tên/email hoặc hash trần thay thế. Các giá trị không có nguồn được đánh dấu bằng trường `has... = false` (mask 0), thay vì ghi số 0 như một quan sát thật.

## 2. Event log theo mục 3.1

| Event schema | Payload chính |
|---|---|
| `session_start` | build, map, patch, device class |
| `segment_start` | segment, objective, applied difficulty |
| `difficulty_applied` | profile/vector, policy, propensity |
| `enemy_spawn` | loại, count, vị trí bucket |
| `enemy_kill` | loại enemy, attacker, thời điểm |
| `player_hit` | damage, source, HP sau hit |
| `tower_build` / `ability_used` | loại đối tượng, cost, vị trí bucket |
| `invalid_action` | loại action, lý do |
| `objective_success` / `objective_failure` | segment, kết quả/lý do |
| `death` / `quit` | segment, thời điểm/lý do |
| `crash` / `disconnect` | segment, nhóm lỗi |
| `segment_end` / `session_end` | outcome, terminal flag/lý do, thời gian |

Event records có schema/session/run/match/map, timestamp UTC, elapsed time, segment/objective và payload. Các ID run/match/session hiện dùng cùng một ID ngẫu nhiên của phiên chơi.

## 3. Implementation hiện tại

`MatchRecorder` hiện giữ file JSON tổng hợp một trận trong `Application.persistentDataPath/MatchLogs/`, đồng thời append từng snapshot vào JSONL trong `Application.persistentDataPath/Telemetry/`. Snapshot ghi ngay khi bắt đầu, theo chu kỳ 5 giây thực, khi kết thúc và khi app pause. `Map1` được cấu hình `saveInterval: 5`; nếu scene gameplay hợp lệ khác chưa có recorder, event logger tự tạo đúng một recorder lúc scene tải.

Snapshot hiện ghi metadata/schema/masks và các trường có dữ liệu: wave, tiến độ theo wave, HP hiện tại/HP khởi đầu, gold, tower count, enemy sống, kills 5 giây, hits 5/30 giây, deaths/leaks 30 giây, action count 5 giây và invalid-action rate. `progressRatio` là số wave đã hoàn thành chia tổng wave, nên là xấp xỉ tiến độ theo quy ước game, không phải tỷ lệ tiến độ thời gian trong một wave.

`actionCount5s` hiện phản ánh những action đã instrument: bắt đầu wave, xây/nâng cấp tháp thành công và các thao tác build/upgrade bị từ chối được ghi là invalid. Nó chưa phải tổng mọi input/UI action. `invalidActionRate` được mask thiếu khi trong cửa sổ 5 giây không có action nào.

### Coverage snapshot

| Trường/nhóm | Trạng thái hiện tại | Ghi chú |
|---|---|---|
| schema, session/run/match/map, build/device | Có | ID session/run/match dùng chung; map là scene name. |
| timestamp UTC, elapsed thực | Có | Chu kỳ mục tiêu 5 giây; có thể trễ theo frame/pause, không nội suy. |
| segment/wave/objective | Có một phần | `segmentId` dạng `wave-01`; objective `defend_core`; wave là ranh giới segment. |
| current difficulty | Không khả dụng | Không có DDA/profile đang áp dụng; `hasCurrentDifficulty=false`. |
| progress ratio | Có, quy ước wave | Completed waves / total waves; không biểu diễn phần trăm tiến độ bên trong wave. |
| HP / HP max | Có, HP max xấp xỉ | `hpMax` lấy HP khởi tạo; đúng với prototype hiện tại chưa có hồi máu/max-HP động. |
| gold, tower count, enemy alive | Có | Đếm trạng thái tại thời điểm snapshot. |
| kills 5s; hits 5/30s; deaths 30s; leaks 30s | Có | Dựa trên event callback gameplay. |
| action count / invalid rate | Có một phần | Chỉ những action được hook; không bao quát mọi thao tác. |
| idle ratio 5s | Không khả dụng | Chưa có theo dõi hoạt động input đủ tin cậy; mask false. |
| score EMA | Không khả dụng | Game chưa có score/rating EMA được định nghĩa; mask false. |
| player hash / patch ID | Không khả dụng | Chưa có backend/server hay cơ chế HMAC/patch metadata; giữ null. |

### Scene/map coverage

- `Map1`: gameplay scene có `GameManager`, `EnemySpawn`, recorder; snapshot interval đã được đặt 5 giây.
- `Map3`: có dependency gameplay; logger tự gắn recorder khi scene được load và đủ dependency. `Map3` hiện chưa nằm trong `EditorBuildSettings`, vì vậy chưa chắc truy cập được trong player build nếu chưa thêm scene vào build settings.
- `Map2`: scene tilemap/art, không có cả cặp `GameManager` + `EnemySpawn`; logger cố ý không bắt đầu gameplay telemetry và không gắn recorder. Không nên coi Map2 là một map chơi có dữ liệu.

## 4. Event nào có và không có?

| Event | Trạng thái | Giới hạn |
|---|---|---|
| `session_start` | Có | Bắt đầu khi scene tên `Map*` đã load và chứa `GameManager` + `EnemySpawn`. |
| `segment_start` | Có | Phát khi bắt đầu wave; segment dạng `wave-XX`, objective `defend_core`; chưa có difficulty. |
| `difficulty_applied` | Chỉ có API hook, chưa phát sinh | Game chưa có profile/controller DDA, candidate set, policy, propensity hay seed. |
| `enemy_spawn` | Có | Loại, count=1, wave, vị trí bucket theo lưới 2 world units. |
| `enemy_kill` | Có một phần | Loại enemy có; attacker lưu `unknown` vì damage/projectile chưa truyền nguồn gây sát thương đến event chết. |
| `player_hit` | Có | Damage/HP sau hit; nguồn hiện tại là enemy lọt tới core (`enemy_leak`). |
| `tower_build` | Có | TowerData, cost, vị trí bucket. |
| `tower_upgrade` | Có | Event bổ sung; tower, level, upgrade cost. |
| `ability_used` | Không có | Prototype chưa có hệ thống ability. |
| `invalid_action` | Có một phần | Có một số lỗi đặt/nâng cấp tháp như vị trí không hợp lệ, thiếu vàng/prefab, nâng cấp tối đa; không bao quát mọi UI action. |
| `objective_success` / `objective_failure` | Có theo wave | Wave clear ghi success; core HP về 0 ghi failure. Đây là operational definition, cần chốt trước thu thập chính thức. |
| `death` | Có | Ghi khi HP core về 0; các leak sau khi HP đã bằng 0 không tạo hit/death trùng lặp. |
| `segment_end` | Có | Đóng wave, kết thúc trận hoặc rời trận. |
| `quit` / `session_end` | Có một phần | Có qua menu/restart/quit và callback quit bình thường; không bảo đảm khi app bị kill đột ngột. |
| `crash` / `disconnect` | Không có | Logger phía client không thể bảo đảm ghi crash/process kill/mất kết nối; cần collector/backend bên ngoài. |
| `playerIdHash`, `patchId` trong event | Không có giá trị | Trường tồn tại nhưng null, không được giả lập. |

## 5. Dữ liệu còn thiếu so với báo cáo nghiên cứu

Không nên dùng log này để khẳng định đã có candidate-conditioned DDA dataset. Hiện prototype chưa áp dụng difficulty và không phát `difficulty_applied`; do đó không có `currentDifficulty`, candidate/applied profile, difficulty vector/mask quan sát thật, policy mode, propensity, random seed, controller/model version. `idleRatio5s`, `scoreEma`, `playerIdHash`, `patchId`, `ability_used`, attacker chính xác, `crash` và `disconnect` cũng chưa có nguồn dữ liệu. Các field thiếu được giữ null hoặc mask 0.

Ngoài ra, action count mới là tập con action; `hpMax` là HP khởi tạo; `progressRatio` dựa trên số wave; objective success được định nghĩa theo wave clear. Cần xác nhận các semantics này với thiết kế nghiên cứu và kiểm tra log bằng Unity Play Mode trước khi dùng thu thập người tham gia.

## 6. Vị trí file và bước xác minh

- Event JSONL: `Application.persistentDataPath/Telemetry/events_<UTC>_<sessionId>.jsonl`.
- Snapshot JSONL: `Application.persistentDataPath/Telemetry/snapshots_<UTC>_<matchId>.jsonl`.
- JSON tổng hợp trận: `Application.persistentDataPath/MatchLogs/match_<UTC>_<matchId>.json`.
- Script: `Assets/_Scenes/Script/TelemetryEventLogger.cs` và `Assets/_Scenes/Script/MatchRecorder.cs`.

Khi test, xác nhận thứ tự `session_start → segment_start → enemy_spawn/tower_build/player_hit/... → segment_end → session_end`, khoảng snapshot gần 5 giây, `sessionId`/`runId`/`matchId` khớp giữa hai file, JSONL mỗi dòng parse riêng được, field thiếu có mask/null đúng, và chỉ Map1/Map3 đủ gameplay dependency được recorder. Môi trường chỉnh sửa không có Unity Editor; compile và Play Mode vẫn cần chạy trong Unity của người dùng.

## 7. Kết luận

Thay đổi đưa hai luồng snapshot/event gần schema hơn và lưu rõ khả dụng của dữ liệu, đặc biệt với snapshot 5 giây và rolling metrics. Nó hữu ích cho kiểm tra instrumentation và baseline telemetry, nhưng **chưa đủ cho huấn luyện hoặc tuyên bố hiệu ứng nhân quả của Candidate-Conditioned DDA** cho tới khi game thật sự áp dụng randomized difficulty, ghi propensity/decision và bổ sung các nguồn dữ liệu còn thiếu.

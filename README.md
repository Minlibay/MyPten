# MyPten

## Настройка модели игрока

1. Импортируй ассет **asoliddev - Low Poly Fantasy Warrior** в папку проекта (любой каталог внутри `Assets`).
2. Открой нужную сцену и выбери объект с компонентом `BattleManager`.
3. В инспекторе в блоке **Player Avatar** укажи поле **Player Avatar Prefab** — перетащи туда нужный префаб воина.
4. Скрипт автоматически опускает модель до земли по габаритам рендера и капсулы. Если требуется своё позиционирование, отключи галочку **Auto Align Player Avatar** и задай смещение вручную через поле **Player Avatar Offset**.
5. В каталоге `Assets/Resources/Blink` размести анимации из пакета Blink. Скрипт `PlayerAnimationDriver` ищет до 32 клипов с названиями:
   - Idle, Combat Idle, Run Forward, Sprint, Run Left, Run Right, Strafe Left, Strafe Right
   - Run Backward, Run Backward Left, Run Backward Right, Jump, Jump while running, Falling Loop
   - Roll Forward/Left/Right/Backward, Punch Left/Right, One-Handed и Two-Handed Melee Attack
   - Magic Attack, Spell Casting Loop, Bow Shot, Buff / Boost, Get Hit, Blocking Loop, Stunned Loop, Death, Gathering, Mining
   Пропущенные клипы можно назначить вручную в инспекторе.
6. Убедись, что в префабе героя есть компонент `Animator`. В этом случае `PlayerAnimationDriver` построит собственный миксер, который выбирает нужные беговые, стрейфовые и обратные клипы, а также отдельный слой для действий (удары, заклинания, перекаты). Если нужных анимаций нет, компонент откатится к классической схеме с параметрами **Speed** и **IsMoving** — их имена можно изменить в инспекторе объекта `Player`.
7. Для ручного вызова анимаций действий используй метод `PlayAction(AnimationKey key)` компонента `PlayerAnimationDriver` — он мягко включает клип на верхнем слое. Зацикленные действия (`SpellCastingLoop`, `BlockingLoop`, `StunnedLoop`, `Gathering`, `Mining`) отключаются методом `StopAction`.

Если префаб не задан, игра автоматически создаст капсулу-заглушку, поэтому проект остаётся рабочим даже без импортированного ассета.

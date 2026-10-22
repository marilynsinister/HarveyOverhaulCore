<!-- Карточка состояния в «Справочнике» (контекст — HandbookRow). Подзаголовки над текстом, а не слева:
     в колонке шириной ~200px подпись «Эффекты:» слева съедала половину строки. -->
<lane orientation="vertical" layout="stretch content">
  <lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle" margin="0,0,0,6">
    <image layout="32px 32px" sprite={HandbookIcon} margin="0,0,8,0" />
    <label font="small" bold="true" text={Title} color="#3b2a1a" layout="stretch content" />
  </lane>

  <label font="small" text="Эффекты" color="#7f6139" />
  <label font="small" text={Effects} layout="stretch content" margin="0,0,0,6" />

  <label font="small" text="Причины" color="#7f6139" />
  <label font="small" text={Causes} layout="stretch content" margin="0,0,0,6" />

  <label font="small" text="Лечение" color="#7f6139" />
  <label font="small" text={CureSummary} layout="stretch content" margin="0,0,0,6" />

  <label font="small" text={StatusText} color={StatusColor} layout="stretch content" />
  <lane *if={HasTreatmentStage} orientation="vertical" layout="stretch content" margin="0,6,0,0">
    <label font="small" text="Сейчас" color="#6b6b6b" />
    <label font="small" text={TreatmentStageText} color="#6b6b6b" layout="stretch content" />
  </lane>
</lane>

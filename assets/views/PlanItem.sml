<!-- Контекст: HarveyPlanItemViewModel. Результат отмечается игрой, иконка состояния — не кнопка. -->
<lane orientation="horizontal" layout="stretch content" margin="0,8,0,8">
  <frame *if={HasActivityIcon} layout="48px 48px" padding="6" margin="0,0,12,0"
         background={@Mods/StardewUI/Sprites/MenuSlotTransparent}>
    <image layout="stretch stretch" sprite={ActivityIcon} />
  </frame>
  <lane orientation="vertical" layout="stretch content">
    <label font="small" bold="true" text={Title} color={TitleColor} layout="stretch content" />
    <lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle" margin="0,4,0,0">
      <image *if={HasStateIcon} layout="20px 20px" sprite={StateIcon} tint={StateTint}
             tooltip={StateTooltip} margin="0,0,6,0" />
      <label font="small" text={StateLabel} color={StateColor} margin="0,0,12,0" />
      <label *if={HasTimingText} font="small" text={TimingText} color="#7f6139" layout="stretch content" />
    </lane>
    <label *if={HasSubtitle} font="small" text={Subtitle} color="#5c4630" layout="stretch content" margin="0,4,0,0" />
    <lane *if={HasProgress} orientation="vertical" layout="stretch content" margin="0,8,0,2">
      <panel layout="stretch 10px">
        <image layout="100% 10px" sprite={BarSprite} tint="#c9a77a" fit="Stretch" />
        <image *if={HasProgressFill} layout={ProgressFillLayout} sprite={BarSprite} tint={ProgressColor} fit="Stretch" />
      </panel>
      <label font="small" text={ProgressText} color="#7f6139" margin="0,4,0,0" />
    </lane>

    <frame *if={ShowDetails} layout="stretch content" padding="12,10,12,10" margin="0,8,0,0"
           background={@Mods/StardewUI/Sprites/MenuSlotTransparent}>
      <lane orientation="vertical" layout="stretch content">
        <lane *if={HasReason} orientation="vertical" layout="stretch content" margin="0,0,0,8">
          <label font="small" bold="true" text="Почему это нужно" color="#7f6139" />
          <label font="small" text={ReasonText} color="#5c4630" layout="stretch content" margin="0,2,0,0" />
        </lane>
        <lane *if={HasNextStep} orientation="vertical" layout="stretch content" margin="0,0,0,8">
          <label font="small" bold="true" text="Следующий шаг" color="#7f6139" />
          <label font="small" text={NextStepText} color="#5c4630" layout="stretch content" margin="0,2,0,0" />
        </lane>
        <label *if={ShowLegacyDetail} font="small" text={DetailText} color="#5c4630" layout="stretch content" />
        <lane *if={ShowRiskText} orientation="vertical" layout="stretch content">
          <label font="small" bold="true" text="Если не соблюдать" color="#8b4513" />
          <label font="small" text={RiskText} color="#8b4513" layout="stretch content" margin="0,2,0,0" />
        </lane>
      </lane>
    </frame>
    <frame *if={HasDetails} layout="content content" padding="10,8,10,8" margin="0,6,0,0"
           background={@Mods/StardewUI/Sprites/MenuSlotTransparent}
           focusable="true" left-click=|ToggleDetails()|>
      <label font="small" text={ToggleLabel} color="#435a97" />
    </frame>
  </lane>
</lane>

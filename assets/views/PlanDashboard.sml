<!-- Контекст: HarveyPlanDashboardViewModel. Режим до сна не входит в шкалу разовых задач. -->
<lane orientation="vertical" layout="stretch content">
  <lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle">
    <frame *if={HasPortrait} layout="72px 72px" padding="4" margin="0,0,14,0"
           background={@Mods/StardewUI/Sprites/MenuSlotInset}>
      <image layout="stretch stretch" sprite={Portrait} />
    </frame>
    <lane orientation="vertical" layout="stretch content">
      <label font="small" text={StatusText} color={StatusColor} margin="0,0,0,4" />
      <label font="small" bold="true" text={MainActionTitle} color="#3b2a1a" layout="stretch content" />
      <label *if={HasMainActionBody} font="small" text={MainActionBody} color="#5c4630"
             layout="stretch content" max-lines="2" tooltip={MainActionBody} margin="0,4,0,0" />
    </lane>
  </lane>

  <frame *if={HasNotice} layout="stretch content" padding="10,8,10,8" margin="0,10,0,0"
         background={@Mods/StardewUI/Sprites/MenuSlotTransparent}>
    <label font="small" text={NoticeText} color="#8b4513" layout="stretch content" />
  </frame>

  <grid *if={HasMetrics} layout="stretch content" item-layout="count: 3" item-spacing="8,8" margin="0,12,0,0">
    <frame *repeat={Metrics} layout="stretch content" padding="10,8,10,8"
           background={@Mods/StardewUI/Sprites/MenuSlotTransparent} tooltip={Tooltip}>
      <lane orientation="vertical" layout="stretch content">
        <lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle">
          <image *if={HasIcon} sprite={Icon} layout="28px 28px" margin="0,0,8,0" />
          <label font="small" bold="true" text={Value} color={Color} layout="stretch content" />
        </lane>
        <label font="small" text={Label} color="#7f6139" layout="stretch content" margin="0,4,0,0" />
      </lane>
    </frame>
  </grid>

  <lane *if={HasProgress} orientation="vertical" layout="stretch content" margin="0,10,0,0">
    <panel layout="stretch 10px">
      <image layout="100% 10px" sprite={BarSprite} tint="#c9a77a" fit="Stretch" />
      <image *if={HasProgressFill} layout={ProgressFillLayout} sprite={BarSprite} tint="#3f7a2a" fit="Stretch" />
    </panel>
    <label font="small" text={ProgressText} color="#7f6139" margin="0,4,0,0" />
  </lane>
</lane>

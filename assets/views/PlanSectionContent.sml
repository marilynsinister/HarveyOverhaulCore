<lane orientation="vertical" layout="stretch content">
  <lane *if={HasDashboard} orientation="vertical" layout="stretch content">
    <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanDashboard" *context={Dashboard} />
  </lane>
  <lane *!if={HasDashboard} orientation="vertical" layout="stretch content">
    <label *if={HasStatusLine} font="small" text={StatusLine} color={StatusColor} layout="stretch content" margin="0,6,0,6" />
    <lane *if={HasProgress} orientation="vertical" layout="stretch content" margin="0,6,0,8">
      <panel layout="stretch 14px">
        <image layout="100% 14px" sprite={BarSprite} tint="#c9a77a" fit="Stretch" />
        <image *if={HasProgressFill} layout={ProgressFillLayout} sprite={BarSprite} tint={ProgressColor} fit="Stretch" />
      </panel>
      <label font="small" text={ProgressText} color="#7f6139" margin="0,4,0,0" />
    </lane>
    <label *if={HasBodyText} font="small" text={BodyText} layout="stretch content" margin="0,6,0,2" />
    <include *repeat={Items} name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanItem" />
    <label *if={HasFootnote} font="small" text={Footnote} color="#7f6139" layout="stretch content" margin="0,6,0,0" />
    <frame *if={HasAction} layout="content 48px" padding="16,0,16,0" margin="0,10,0,0"
           background={@Mods/StardewUI/Sprites/ButtonLight} vertical-content-alignment="middle"
           focusable="true" left-click=|RunAction()|>
      <label font="small" text={ActionLabel} color="#3b2a1a" max-lines="1" />
    </frame>
  </lane>
</lane>

<frame layout="stretch content" background={FrameBackground} border={FrameBorder}
       border-thickness={FrameBorderThickness} padding={FramePadding} margin="0,0,0,12">
  <lane orientation="vertical" layout="stretch content">
    <expander *if={IsCollapsible} layout="stretch content" header-padding="0,6,0,6" is-expanded={<>Expanded}>
      <lane *outlet="header" orientation="vertical" layout="stretch content">
        <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanSectionHeader" />
      </lane>
      <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanSectionContent" />
    </expander>
    <lane *!if={IsCollapsible} orientation="vertical" layout="stretch content">
      <include *!if={HasDashboard} name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanSectionHeader" />
      <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanSectionContent" />
    </lane>
  </lane>
</frame>

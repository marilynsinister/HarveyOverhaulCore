<frame layout="100% content" background={@Mods/StardewUI/Sprites/MenuSlotTransparent}
       padding="10,8,10,8" margin="0,10,0,0">
  <lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle">
    <frame *if={HasAdvicePortrait} layout="48px 48px" padding="4" margin="0,0,12,0"
           background={@Mods/StardewUI/Sprites/MenuSlotInset}>
      <image layout="stretch stretch" sprite={AdvicePortrait} />
    </frame>
    <lane orientation="vertical" layout="stretch content">
      <label font="small" bold="true" text="Совет Харви" color="#7f6139" margin="0,0,0,2" />
      <label font="small" text={HarveyAdviceText} layout="stretch content" max-lines="3" tooltip={HarveyAdviceText} />
    </lane>
  </lane>
</frame>

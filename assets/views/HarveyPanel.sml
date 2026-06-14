<frame
  layout="900px 620px"
  background={@Mods/StardewUI/Sprites/MenuBackground}
  border={@Mods/StardewUI/Sprites/MenuBorder}
  border-thickness="36,36,40,36"
  padding="24,20,24,20">
  <lane orientation="vertical" layout="stretch stretch">
    <banner
      background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0,48,0"
      padding="10,6"
      margin="0,0,8,0"
      text="{Title}" />

    <lane orientation="horizontal" layout="stretch content" margin="0,0,10,0">
      <tab *repeat="{Tabs}"
        layout="108px"
        text="{Label}"
        active="{<>Active}"
        group-key="harvey-panel-tabs"
        activate=|^SelectTab(Key)| />
    </lane>

    <label font="dialogue" text="{ActiveTabTitle}" margin="0,0,8,0" />

    <scrollable layout="stretch stretch" peeking="32">
      <lane orientation="vertical">
        <frame *repeat="{ActiveSections}"
          layout="stretch content"
          background={@Mods/StardewUI/Sprites/MenuBackground}
          border={@Mods/StardewUI/Sprites/MenuBorder}
          border-thickness="12,12,12,12"
          padding="10,10,10,10"
          margin="0,0,8,0">
          <lane orientation="vertical">
            <label font="dialogue" text="{Title}" margin="0,0,4,0" />
            <label font="small" text="{Status}" color="#7f6139" margin="0,0,4,0" />
            <label font="small" text="{Body}" />
          </lane>
        </frame>

        <lane *if="{ShowStressHandbook}" orientation="vertical" margin="0,4,0,0">
          <label font="small" text="Сейчас" color="#7f6139" margin="0,0,6,0" />
          <scrollable peeking="32">
            <grid layout="stretch content" item-layout="length: 260">
              <frame *repeat="{Handbook.ActiveStates}"
                layout="260px 200px"
                background={@Mods/StardewUI/Sprites/MenuBackground}
                border={@Mods/StardewUI/Sprites/MenuBorder}
                border-thickness="12,12,12,12"
                padding="8,8,8,8">
                <lane orientation="vertical">
                  <lane orientation="horizontal">
                    <image layout="40px 40px" texture="{IconSprite.Texture}" source-rect="{IconSprite.SourceRect}" margin="6,8,0,0" />
                    <label font="dialogue" text="{Title}" />
                  </lane>
                  <label font="small" text="{Effects}" />
                  <label font="small" text="{Causes}" />
                  <label font="small" text="{CureSummary}" />
                  <label font="small" text="{StatusText}" color="{StatusColor}" margin="0,4,0,0" />
                  <label font="small" text="Сейчас: {TreatmentStageText}" color="#6b6b6b" />
                </lane>
              </frame>
              <image layout="8px 1px" />
            </grid>
          </scrollable>

          <label font="small" text="Справочник" color="#7f6139" margin="8,0,6,0" />
          <scrollable peeking="32">
            <grid layout="stretch content" item-layout="count: 2">
              <frame *repeat="{Handbook.AllStates}"
                background={@Mods/StardewUI/Sprites/MenuBackground}
                border={@Mods/StardewUI/Sprites/MenuBorder}
                border-thickness="12,12,12,12"
                padding="8,8,8,8"
                margin="0,0,8,8">
                <lane orientation="vertical">
                  <lane orientation="horizontal">
                    <image layout="40px 40px" texture="{IconSprite.Texture}" source-rect="{IconSprite.SourceRect}" margin="0,0,6,0" />
                    <label font="dialogue" text="{Title}" />
                  </lane>
                  <label font="small" text="{Effects}" />
                  <label font="small" text="{Causes}" />
                  <label font="small" text="{CureSummary}" />
                  <label font="small" text="{StatusText}" color="{StatusColor}" margin="0,4,0,0" />
                  <label font="small" text="Сейчас: {TreatmentStageText}" color="#6b6b6b" />
                </lane>
              </frame>
            </grid>
          </scrollable>
        </lane>
      </lane>
    </scrollable>

    <label font="small" text="Совет Харви:" color="#7f6139" margin="8,4,0,0" />
    <label font="small" text="{HarveyAdviceText}" margin="0,0,0,0" />
  </lane>
</frame>

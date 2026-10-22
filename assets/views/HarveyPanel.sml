<!-- Порядок значений margin/padding в StardewUI: left, top, right, bottom (не как в CSS). -->
<frame
  layout={PanelLayout}
  background={@Mods/StardewUI/Sprites/MenuBackground}
  border={@Mods/StardewUI/Sprites/MenuBorder}
  border-thickness={PanelBorderThickness}
  padding={PanelPadding}>
  <lane orientation="vertical" layout="stretch stretch">
    <banner
      background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0,48,0"
      padding={BannerPadding}
      margin="0,0,0,10"
      text={Title} />

    <!-- Вкладки — кнопки на nine-slice спрайтах, ширина по собственной подписи (HarveyPanelLayout).
         Если в один ряд не помещаются — переносятся во второй, а не уходят за край окна. -->
    <lane orientation="vertical" layout="stretch content" margin="0,0,0,10">
      <lane *repeat={TabRows}
            orientation="horizontal"
            layout="stretch content"
            margin="0,0,0,4"
            horizontal-content-alignment="middle">
        <panel *repeat={Tabs} margin="4,0,4,0">
          <frame *if={Active}
                 layout={ButtonLayout}
                 background={@Mods/StardewUI/Sprites/ButtonLight}
                 horizontal-content-alignment="middle"
                 vertical-content-alignment="middle"
                 focusable="true"
                 left-click=|^^SelectTab(Key)|>
            <lane orientation="horizontal" vertical-content-alignment="middle">
              <image *if={HasTabIcon} layout="24px 24px" sprite={TabIcon} margin="0,0,6,0" />
              <label font="small" text={Label} color={TabTextColor} max-lines="1" />
            </lane>
          </frame>
          <frame *!if={Active}
                 layout={ButtonLayout}
                 background={@Mods/StardewUI/Sprites/ButtonDark}
                 opacity="0.75"
                 horizontal-content-alignment="middle"
                 vertical-content-alignment="middle"
                 focusable="true"
                 left-click=|^^SelectTab(Key)|>
            <lane orientation="horizontal" vertical-content-alignment="middle">
              <image *if={HasTabIcon} layout="24px 24px" sprite={TabIcon} margin="0,0,6,0" />
              <label font="small" text={Label} color={TabTextColor} max-lines="1" />
            </lane>
          </frame>
        </panel>
      </lane>
    </lane>

    <scrollable layout="stretch stretch" peeking="48">
      <!-- Правый отступ 12px — чтобы карточки не прижимались к полосе прокрутки. -->
      <lane orientation="vertical" layout="stretch content" padding="0,4,12,8">
        <label *if={ShowPlanTabContent} font="small" text={PlanDetailBody} layout="stretch content" margin="0,0,0,12" />

        <include *repeat={ActiveSections} name="Mods/marilynsinister.HarveyOverhaul.Core/Views/PlanSection" />

        <!-- Справочник. Раньше здесь были вложенные scrollable (прокрутка внутри прокрутки) и карточки
             фиксированной ширины 260px — на узком окне они вылезали за край. -->
        <lane *if={ShowStressHandbook} orientation="vertical" layout="stretch content" margin="0,0,0,4">
          <label font="small" bold="true" text="Сейчас" color="#7f6139" margin="0,0,0,8" />
          <grid layout="stretch content" item-layout={HandbookGridLayout} item-spacing="12,12">
            <frame *repeat={Handbook.ActiveStates}
              layout="stretch content"
              background={@Mods/StardewUI/Sprites/MenuBackground}
              border={@Mods/StardewUI/Sprites/MenuBorder}
              border-thickness="12,12,12,12"
              padding="14,12,14,12">
              <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/HandbookCard" />
            </frame>
          </grid>

          <label font="small" bold="true" text="Справочник" color="#7f6139" margin="0,16,0,8" />
          <grid layout="stretch content" item-layout={HandbookGridLayout} item-spacing="12,12">
            <frame *repeat={Handbook.AllStates}
              layout="stretch content"
              background={@Mods/StardewUI/Sprites/MenuBackground}
              border={@Mods/StardewUI/Sprites/MenuBorder}
              border-thickness="12,12,12,12"
              padding="14,12,14,12">
              <include name="Mods/marilynsinister.HarveyOverhaul.Core/Views/HandbookCard" />
            </frame>
          </grid>
        </lane>
        <include *if={ShowInlineAdvice} name="Mods/marilynsinister.HarveyOverhaul.Core/Views/HarveyAdvice" />
      </lane>
    </scrollable>

    <include *if={ShowAdviceFooter} name="Mods/marilynsinister.HarveyOverhaul.Core/Views/HarveyAdvice" />
  </lane>
</frame>

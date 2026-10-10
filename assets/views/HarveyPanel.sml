<frame
  layout="1040px 760px"
  background={@Mods/StardewUI/Sprites/MenuBackground}
  border={@Mods/StardewUI/Sprites/MenuBorder}
  border-thickness="36,36,40,36"
  padding="32,32,32,32">
  <lane orientation="vertical" layout="stretch stretch">
    <banner
      background={@Mods/StardewUI/Sprites/BannerBackground}
      background-border-thickness="48,0,48,0"
      padding="18,18,12,12"
      margin="0,0,0,14"
      text={Title} />

    <!-- Вкладки — кнопки на nine-slice спрайтах. Стандартный <tab> рисует TabTopEmpty фиксированной
         ширины (~64px) и не растягивается: подписи вылезали за дугу и переносились («Травм-ы»). -->
    <lane orientation="horizontal"
          layout="stretch content"
          margin="0,0,0,16"
          horizontal-content-alignment="middle">
      <panel *repeat={Tabs} margin="4,0,4,0">
        <frame *if={Active}
               layout="160px 56px"
               background={@Mods/StardewUI/Sprites/ButtonLight}
               horizontal-content-alignment="middle"
               vertical-content-alignment="middle"
               focusable="true"
               left-click=|^SelectTab(Key)|>
          <lane orientation="horizontal" vertical-content-alignment="middle">
            <image *if={HasTabIcon} layout="24px 24px" sprite={TabIcon} margin="0,0,6,0" />
            <label font="small" text={Label} color={TabTextColor} max-lines="1" />
          </lane>
        </frame>
        <frame *!if={Active}
               layout="160px 56px"
               background={@Mods/StardewUI/Sprites/ButtonDark}
               opacity="0.8"
               horizontal-content-alignment="middle"
               vertical-content-alignment="middle"
               focusable="true"
               left-click=|^SelectTab(Key)|>
          <lane orientation="horizontal" vertical-content-alignment="middle">
            <image *if={HasTabIcon} layout="24px 24px" sprite={TabIcon} margin="0,0,6,0" />
            <label font="small" text={Label} color={TabTextColor} max-lines="1" />
          </lane>
        </frame>
      </panel>
    </lane>

    <scrollable layout="stretch stretch" peeking="32">
      <lane orientation="vertical" layout="stretch content" padding="0,8,16,0">
        <label *if={ShowPlanTabContent} font="small" text={PlanDetailBody} layout="stretch content" margin="0,0,12,12" />

        <frame *repeat={ActiveSections}
          layout="stretch content"
          background={@Mods/StardewUI/Sprites/MenuBackground}
          border={@Mods/StardewUI/Sprites/MenuBorder}
          border-thickness="12,12,12,12"
          padding="20,20,18,18"
          margin="0,0,0,12">
          <lane orientation="vertical" layout="stretch content">
            <lane *if={HasHeadline} orientation="horizontal" layout="stretch content" vertical-content-alignment="middle" margin="0,0,0,6">
              <image *if={HasIcon} layout="32px 32px" sprite={Icon} margin="0,0,10,0" />
              <label font="dialogue" text={Headline} color={AccentColor} layout="stretch content" />
            </lane>
            <label *if={HasStatusLine} font="small" text={StatusLine} layout="stretch content" color={StatusColor} margin="0,0,0,6" />
            <label *if={HasBodyText} font="small" text={BodyText} layout="stretch content" margin="0,0,0,2" />
          </lane>
        </frame>

        <lane *if={ShowStressHandbook} orientation="vertical" layout="stretch content" margin="0,0,4,0">
          <label font="small" text="Сейчас" color="#7f6139" margin="0,0,0,8" />
          <scrollable layout="stretch content" peeking="32">
            <lane orientation="vertical" layout="stretch content" padding="0,16,0,0">
            <grid layout="stretch content" item-layout="length: 260">
              <frame *repeat={Handbook.ActiveStates}
                layout="260px content"
                background={@Mods/StardewUI/Sprites/MenuBackground}
                border={@Mods/StardewUI/Sprites/MenuBorder}
                border-thickness="12,12,12,12"
                padding="16,16,14,14">
                <lane orientation="vertical" layout="stretch content">
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,6">
                    <image layout="40px 40px" sprite={HandbookIcon} margin="0,8,0,0" />
                    <label font="dialogue" text={Title} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Эффекты:" margin="0,6,0,0" />
                    <label font="small" text={Effects} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Причины:" margin="0,6,0,0" />
                    <label font="small" text={Causes} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Лечение:" margin="0,6,0,0" />
                    <label font="small" text={CureSummary} layout="stretch content" />
                  </lane>
                  <label font="small" text={StatusText} layout="stretch content" color="#7f6139" margin="0,0,6,0" />
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,4,0">
                    <label font="small" text="Сейчас:" color="#6b6b6b" margin="0,6,0,0" />
                    <label font="small" text={TreatmentStageText} layout="stretch content" color="#6b6b6b" />
                  </lane>
                </lane>
              </frame>
              <image layout="8px 1px" />
            </grid>
            </lane>
          </scrollable>

          <label font="small" text="Справочник" color="#7f6139" margin="0,0,12,8" />
          <scrollable layout="stretch content" peeking="32">
            <lane orientation="vertical" layout="stretch content" padding="0,16,0,0">
            <grid layout="stretch content" item-layout="count: 2">
              <frame *repeat={Handbook.AllStates}
                layout="stretch content"
                background={@Mods/StardewUI/Sprites/MenuBackground}
                border={@Mods/StardewUI/Sprites/MenuBorder}
                border-thickness="12,12,12,12"
                padding="16,16,14,14"
                margin="0,10,0,10">
                <lane orientation="vertical" layout="stretch content">
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,6">
                    <image layout="40px 40px" sprite={HandbookIcon} margin="0,8,0,0" />
                    <label font="dialogue" text={Title} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Эффекты:" margin="0,6,0,0" />
                    <label font="small" text={Effects} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Причины:" margin="0,6,0,0" />
                    <label font="small" text={Causes} layout="stretch content" />
                  </lane>
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,0,4">
                    <label font="small" text="Лечение:" margin="0,6,0,0" />
                    <label font="small" text={CureSummary} layout="stretch content" />
                  </lane>
                  <label font="small" text={StatusText} layout="stretch content" color="#7f6139" margin="0,0,6,0" />
                  <lane orientation="horizontal" layout="stretch content" margin="0,0,4,0">
                    <label font="small" text="Сейчас:" color="#6b6b6b" margin="0,6,0,0" />
                    <label font="small" text={TreatmentStageText} layout="stretch content" color="#6b6b6b" />
                  </lane>
                </lane>
              </frame>
            </grid>
            </lane>
          </scrollable>
        </lane>
      </lane>
    </scrollable>

    <lane orientation="vertical" layout="stretch content" margin="0,0,18,0" padding="8,8,0,0">
      <lane *if={HasPlanAdvice} orientation="horizontal" layout="stretch content" vertical-content-alignment="middle">
        <frame *if={HasAdvicePortrait}
               layout="56px 56px"
               padding="4,4,4,4"
               margin="0,0,12,0"
               background={@Mods/StardewUI/Sprites/MenuSlotInset}>
          <image layout="stretch stretch" sprite={AdvicePortrait} />
        </frame>
        <lane orientation="vertical" layout="stretch content">
          <label font="small" text="Совет Харви:" color="#7f6139" margin="0,0,0,4" />
          <label font="small" text={HarveyAdviceText} layout="stretch content" max-lines="3" />
        </lane>
      </lane>
    </lane>
  </lane>
</frame>

<lane orientation="horizontal" layout="stretch content" vertical-content-alignment="middle">
  <image *if={HasIcon} layout={IconLayout} sprite={Icon} margin="0,0,10,0" />
  <label *if={IsHero} font="dialogue" text={Headline} color={AccentColor} layout="stretch content" />
  <label *!if={IsHero} font="small" bold="true" text={Headline} color={AccentColor} layout="stretch content" />
  <label *if={HasItems} font="small" text={ItemCountText} color="#7f6139" margin="12,0,0,0" />
</lane>

# After creating libmruby.a, convert it to a shared library for Unity. See `mrbgem.rake`.
MRuby::CrossBuild.new("windows") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'
  cc.defines = %w(MRB_NO_BOXING MRB_NO_STDIO)
end

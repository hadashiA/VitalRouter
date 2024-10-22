# After creating libmruby.a, convert it to a shared library for Unity. See `mrbgem.rake`.

MRuby::CrossBuild.new("macos-arm64") do |conf|
  conf.toolchain :clang
  conf.gembox '../../../vitalrouter'

  conf.disable_presym
  conf.cc.defines = %w(MRB_WORD_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
  conf.cc.flags << '-arch arm64'
  conf.cc.flags << '-Os'
  conf.linker.flags << '-arch arm64'
end

MRuby::CrossBuild.new("macos-x64") do |conf|
  conf.toolchain :clang
  conf.gembox '../../../vitalrouter'

  conf.disable_presym
  conf.cc.defines = %w(MRB_WORD_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
  conf.cc.flags << '-arch x86_64'
  conf.linker.flags << '-arch x86_64'
end

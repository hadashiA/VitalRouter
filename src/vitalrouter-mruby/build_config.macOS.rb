# After creating libmruby.a, convert it to a shared library for Unity. See `mrbgem.rake`.

MRuby::CrossBuild.new("macos-arm64") do |conf|
  conf.toolchain :clang
  conf.gembox '../../../vitalrouter'

  conf.cc.defines = << 'MRB_NO_BOXING'    
  conf.cc.flags << '-arch arm64'
  conf.linker.flags << '-arch arm64'
end

MRuby::CrossBuild.new("macos-x64") do |conf|
  conf.toolchain :clang
  conf.gembox '../../../vitalrouter'

  conf.cc.defines = << 'MRB_NO_BOXING'    
  conf.cc.flags << '-arch x86_64'
  conf.linker.flags << '-arch x86_64'
end

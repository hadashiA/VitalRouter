# After creating libmruby.a, convert it to a shared library for Unity. See `mrbgem.rake`.
MRuby::CrossBuild.new('android-arm64') do |conf|
  toolchain :android, arch: 'arm64-v8a'  
  conf.gembox '../../../vitalrouter'
  
  conf.cc.defines = << 'MRB_NO_BOXING'
end

MRuby::CrossBuild.new('android-x64') do |conf|
  toolchain :android, arch: 'x86_64'
  conf.gembox '../../../vitalrouter'

  conf.cc.defines = << 'MRB_NO_BOXING'  
end

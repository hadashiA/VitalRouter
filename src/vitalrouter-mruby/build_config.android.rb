# After creating libmruby.a, convert it to a shared library for Unity. See `mrbgem.rake`.
MRuby::CrossBuild.new('android-arm64') do |conf|
  toolchain :android, arch: 'arm64-v8a'  
  conf.gembox '../../../vitalrouter'

  conf.disable_presym  
  conf.cc.defines = %w(MRB_NO_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
  conf.cc.flags << '-Os'
end

MRuby::CrossBuild.new('android-x64') do |conf|
  toolchain :android, arch: 'x86_64'
  conf.gembox '../../../vitalrouter'

  conf.disable_presym    
  conf.cc.defines = %w(MRB_NO_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
  conf.cc.flags << '-Os'
end

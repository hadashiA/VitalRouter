MRuby::CrossBuild.new("macOS-arm64") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.cc.defines << 'MRB_NO_STDIO'
  conf.cc.flags << '-arch arm64'
  conf.linker.flags << '-arch x86_64'  
end

MRuby::CrossBuild.new("macOS-x64") do |conf|
  conf.toolchain :clang
  conf.gembox '../../../vitalrouter'

  conf.cc.defines << 'MRB_NO_STDIO'
  conf.cc.flags << '-arch x86_64'
  conf.linker.flags << '-arch x86_64'
end

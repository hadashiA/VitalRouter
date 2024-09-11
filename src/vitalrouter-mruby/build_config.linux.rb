# For x64 linux machine
MRuby::CrossBuild.new("linux-x64") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'
end

MRuby::CrossBuild.new("linux-arm64") do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.cc.command = 'aarch64-linux-gnu-gcc'
  conf.linker.command = 'aarch64-linux-gnu-gcc'
  conf.archiver.command = 'aarch64-linux-gnu-ar'
end

MRuby::CrossBuild.new('wasm') do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.disable_presym
  conf.cc.defines = %w(MRB_NO_BOXING MRB_NO_STDIO MRB_NO_PRESYM)
  conf.cc.flags << '-Os'
  conf.cc.command = 'emcc'
  conf.linker.command = 'emcc'
  conf.archiver.command = 'emar'
end

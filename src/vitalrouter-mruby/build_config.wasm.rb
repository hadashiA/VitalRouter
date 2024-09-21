MRuby::CrossBuild.new('wasm') do |conf|
  conf.toolchain
  conf.gembox '../../../vitalrouter'

  conf.cc.defines = %w(MRB_NO_BOXING MRB_NO_STDIO)
  conf.cc.command = 'emcc'
  conf.linker.command = 'emcc'
  conf.archiver.command = 'emar'
end
